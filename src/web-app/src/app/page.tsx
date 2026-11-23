"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowRight,
  BookOpen,
  Bot,
  Brain,
  Flame,
  Gamepad2,
  NotebookTabs,
  RefreshCw,
  RotateCcw,
  Sparkles,
  Swords,
  Target,
  Trophy,
  Users,
  Volume2,
  Zap,
} from "lucide-react";
import { useAuth } from "@/components/auth/useAuth";
import { fetchUserProfileSummary, type UserProfileSummaryDto } from "@/lib/api";
import { readQuizProgress, type QuizProgressRecord } from "@/lib/quiz-progress";
import { useLanguage } from "@/lib/language-context";
import type { VocabularyItem } from "@/components/FlashcardStudy";

type VocabularyResponse = { total?: number; words?: VocabularyItem[] };
type GroupsResponse = { groups?: { word_count?: number }[] };
type ReviewsResponse = { totalLearned?: number; totalDue?: number };
type WeeklyDayDto = {
  date?: string;
  words?: number;
  xpEarned?: number;
  isToday?: boolean;
};
type WeeklyActivityResponse = { days?: WeeklyDayDto[] };
type LeaderboardEntry = {
  rank: number;
  user_id: string;
  name: string;
  xp: number;
  level?: string;
  words_learned?: number;
};

const DAILY_WORDS_TARGET = 10;
const DAILY_XP_TARGET = 50;

const readLeaderboard = (payload: unknown): LeaderboardEntry[] => {
  let value = payload;
  if (!Array.isArray(value) && value && typeof value === "object" && "data" in value) {
    value = (value as { data: unknown }).data;
  }
  if (!Array.isArray(value) && value && typeof value === "object" && "entries" in value) {
    value = (value as { entries: unknown }).entries;
  }
  if (!Array.isArray(value)) return [];
  return value
    .filter(
      (item): item is LeaderboardEntry =>
        Boolean(
          item &&
            typeof item === "object" &&
            typeof (item as LeaderboardEntry).name === "string" &&
            Number.isFinite((item as LeaderboardEntry).xp)
        )
    )
    .sort((a, b) => b.xp - a.xp)
    .map((item, index) => ({ ...item, rank: index + 1 }));
};

function getTodayDateSeed(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function hashSeed(seed: string): number {
  let h = 2166136261;
  for (let i = 0; i < seed.length; i += 1) {
    h ^= seed.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return Math.abs(h);
}

export default function HomePage() {
  const { user, isAuthenticated } = useAuth();
  const { translate: t, language } = useLanguage();
  const [sessions, setSessions] = useState<QuizProgressRecord[]>([]);
  const [vocabularyPool, setVocabularyPool] = useState<VocabularyItem[]>([]);
  const [vocabularyTotal, setVocabularyTotal] = useState<number | null>(null);
  const [wotdOffset, setWotdOffset] = useState(0);
  const [isSpeakingWotd, setIsSpeakingWotd] = useState(false);
  const [activityRange, setActivityRange] = useState<7 | 30>(7);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [liveSummary, setLiveSummary] = useState<UserProfileSummaryDto | null>(null);
  const [weeklyApiDays, setWeeklyApiDays] = useState<WeeklyDayDto[]>([]);
  const [reviewStats, setReviewStats] = useState<{ totalLearned: number; totalDue: number }>({
    totalLearned: 0,
    totalDue: 0,
  });

  const refresh = useCallback(() => setSessions(readQuizProgress()), []);

  useEffect(() => {
    const initialRefresh = window.setTimeout(refresh, 0);
    window.addEventListener("lingual:quiz-progress", refresh);
    const controller = new AbortController();

    void fetch("/api/vocabulary?limit=100", { signal: controller.signal, cache: "no-store" })
      .then((response) => (response.ok ? (response.json() as Promise<VocabularyResponse>) : null))
      .then((data) => {
        if (data && Array.isArray(data.words)) {
          setVocabularyPool(data.words);
        }
      })
      .catch(() => {});

    void fetch("/api/vocabulary/groups", { signal: controller.signal, cache: "no-store" })
      .then((response) => (response.ok ? (response.json() as Promise<GroupsResponse>) : null))
      .then((data) => {
        if (data?.groups && Array.isArray(data.groups)) {
          const sum = data.groups.reduce((acc, g) => acc + (Number(g.word_count) || 0), 0);
          if (sum > 0) setVocabularyTotal(sum);
        }
      })
      .catch(() => {});

    void fetch("/api/leaderboard", { cache: "no-store", signal: controller.signal })
      .then((response) => (response.ok ? response.json() : null))
      .then((data) => {
        const entries = readLeaderboard(data);
        setLeaderboard(entries);
      })
      .catch(() => {});

    return () => {
      window.clearTimeout(initialRefresh);
      window.removeEventListener("lingual:quiz-progress", refresh);
      controller.abort();
    };
  }, [refresh]);

  useEffect(() => {
    if (!isAuthenticated || !user?.id) {
      return;
    }

    let active = true;
    const controller = new AbortController();
    const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
    const authHeaders: Record<string, string> = token ? { authorization: `Bearer ${token}` } : {};

    void fetchUserProfileSummary(user.id)
      .then((summary) => {
        if (active) setLiveSummary(summary);
      })
      .catch(() => {});

    void fetch("/api/activity/weekly", {
      headers: authHeaders,
      cache: "no-store",
      signal: controller.signal,
    })
      .then((res) => (res.ok ? (res.json() as Promise<WeeklyActivityResponse>) : null))
      .then((data) => {
        if (active && data?.days && Array.isArray(data.days)) {
          setWeeklyApiDays(data.days);
        }
      })
      .catch(() => {});

    if (token) {
      void fetch("/api/reviews", {
        headers: authHeaders,
        cache: "no-store",
        signal: controller.signal,
      })
        .then((res) => (res.ok ? (res.json() as Promise<ReviewsResponse>) : null))
        .then((data) => {
          if (active && data) {
            setReviewStats({
              totalLearned: Number(data.totalLearned) || 0,
              totalDue: Number(data.totalDue) || 0,
            });
          }
        })
        .catch(() => {});
    }

    return () => {
      active = false;
      controller.abort();
    };
  }, [isAuthenticated, user?.id]);

  const activeLiveSummary = isAuthenticated && user?.id ? liveSummary : null;
  const activeWeeklyApiDays = useMemo(
    () => (isAuthenticated && user?.id ? weeklyApiDays : []),
    [isAuthenticated, user?.id, weeklyApiDays]
  );
  const activeReviewStats = useMemo(
    () => (isAuthenticated && user?.id ? reviewStats : { totalLearned: 0, totalDue: 0 }),
    [isAuthenticated, user?.id, reviewStats]
  );

  // Deterministic Word of the Day seeded by YYYY-MM-DD
  const wordOfTheDay = useMemo(() => {
    if (!vocabularyPool.length) return null;
    const baseIdx = hashSeed(getTodayDateSeed()) % vocabularyPool.length;
    return vocabularyPool[(baseIdx + wotdOffset) % vocabularyPool.length];
  }, [vocabularyPool, wotdOffset]);

  const pronounceWotd = useCallback(() => {
    if (!wordOfTheDay || typeof window === "undefined" || !("speechSynthesis" in window)) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(wordOfTheDay.term);
    utterance.lang = "en-US";
    utterance.rate = 0.88;
    utterance.onstart = () => setIsSpeakingWotd(true);
    utterance.onend = () => setIsSpeakingWotd(false);
    utterance.onerror = () => setIsSpeakingWotd(false);
    window.speechSynthesis.speak(utterance);
  }, [wordOfTheDay]);

  const weekly = useMemo(() => {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() - 6);
    return sessions.filter((session) => new Date(session.completedAt) >= start);
  }, [sessions]);

  const todayStats = useMemo(() => {
    const todayStr = new Date().toDateString();
    const todaySessions = sessions.filter(
      (session) => new Date(session.completedAt).toDateString() === todayStr
    );
    const quizWordsToday = todaySessions.reduce((sum, s) => sum + s.correctAnswers, 0);
    const quizXpToday = todaySessions.reduce((sum, s) => sum + s.xp, 0);

    const apiToday = activeWeeklyApiDays.find((d) => d.isToday);
    const apiWordsToday = Number(apiToday?.words) || 0;
    const apiXpToday = Number(apiToday?.xpEarned) || 0;

    return {
      reviewedToday: Math.max(apiWordsToday, quizWordsToday),
      xpToday: Math.max(apiXpToday, quizXpToday),
    };
  }, [sessions, activeWeeklyApiDays]);

  const activityDays = useMemo(
    () =>
      Array.from({ length: activityRange }, (_, offset) => {
        const date = new Date();
        date.setHours(0, 0, 0, 0);
        date.setDate(date.getDate() - (activityRange - 1 - offset));
        const isoDay = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
        const localWords = sessions
          .filter((session) => new Date(session.completedAt).toDateString() === date.toDateString())
          .reduce((total, session) => total + session.correctAnswers, 0);
        const apiMatch = activeWeeklyApiDays.find((d) => d.date && d.date.startsWith(isoDay));
        const apiWords = Number(apiMatch?.words) || 0;
        const apiXp = Number(apiMatch?.xpEarned) || 0;
        return {
          label:
            activityRange === 7
              ? new Intl.DateTimeFormat(language === "vi" ? "vi-VN" : "en-US", {
                  weekday: "short",
                }).format(date)
              : String(date.getDate()),
          count: Math.max(localWords, apiWords),
          apiXp,
          date,
        };
      }),
    [sessions, activityRange, language, activeWeeklyApiDays]
  );

  const rangeXp = useMemo(() => {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() - (activityRange - 1));
    const localXp = sessions
      .filter((session) => new Date(session.completedAt) >= start)
      .reduce((total, item) => total + item.xp, 0);
    const apiXpSum = activityDays.reduce((total, d) => total + d.apiXp, 0);
    return Math.max(localXp, apiXpSum);
  }, [sessions, activityRange, activityDays]);

  const totalRangeWords = useMemo(
    () => activityDays.reduce((sum, day) => sum + day.count, 0),
    [activityDays]
  );

  const learnedQuizzes = sessions.length;
  const effectiveVocabCount = vocabularyTotal ?? (vocabularyPool.length > 0 ? vocabularyPool.length : null);
  const unlockedCount =
    Number(learnedQuizzes > 0 || activeReviewStats.totalLearned > 0) +
    Number(sessions.some((item) => item.totalQuestions > 0 && item.correctAnswers === item.totalQuestions)) +
    Number(learnedQuizzes >= 5) +
    Number(activeReviewStats.totalLearned >= 10);

  const currentEntry = leaderboard.find(
    (entry) => entry.user_id === user?.id || entry.user_id === user?.mezonUserId
  );

  const locale = language === "vi" ? "vi-VN" : "en-US";
  const wordsPct = Math.min(100, Math.round((todayStats.reviewedToday / DAILY_WORDS_TARGET) * 100));
  const xpPct = Math.min(100, Math.round((todayStats.xpToday / DAILY_XP_TARGET) * 100));

  return (
    <main className="dashboard-page">
      {/* 1. HERO WITH WARM GRADIENT, DAILY GOAL PROGRESS BAR & ENGLISH ACCENT BADGE */}
      <section className="dashboard-hero">
        <div className="dashboard-welcome">
          <p className="dashboard-eyebrow">
            <Sparkles size={14} /> {t("KHÔNG GIAN HỌC CỦA BẠN", "YOUR LEARNING SPACE")}
          </p>
          <h1>
            {user?.displayName
              ? (() => {
                  const parts = user.displayName.trim().split(/\s+/);
                  const callName = language === "vi" ? parts[parts.length - 1] : parts[0];
                  return `${t("Chào", "Hello")} ${callName},`;
                })()
              : t("Chào bạn,", "Hello,")}
            <br />
            <em>{t("sẵn sàng học chưa?", "ready to learn?")}</em>
          </h1>
          <p>
            {t(
              "Mỗi phiên học nhỏ đưa bạn đến gần hơn với sự tự tin khi dùng tiếng Anh.",
              "Every small session brings you closer to confidence in English."
            )}
          </p>

          <div className="dashboard-hero-actions">
            <Link className="dashboard-primary" href="/learn">
              {t("Bắt đầu ôn tập", "Start reviewing")} <ArrowRight size={16} />
            </Link>
            <Link className="dashboard-text-link" href="/quiz">
              {t("Làm quiz", "Take a quiz")} <ArrowRight size={15} />
            </Link>
          </div>

          {/* Daily Goal Progress Bar (Real API + Daily Sessions) */}
          <div className="dashboard-daily-goal" aria-label={t("Mục tiêu hôm nay", "Today's goal")}>
            <div className="daily-goal-header">
              <span className="daily-goal-title">
                <Target size={14} aria-hidden="true" /> {t("Mục tiêu hôm nay", "Today's goal")}
              </span>
              {activeReviewStats.totalDue > 0 && (
                <Link href="/words" className="daily-goal-due-link">
                  {t(`${activeReviewStats.totalDue} thẻ đến hạn ôn`, `${activeReviewStats.totalDue} cards due`)}
                </Link>
              )}
            </div>
            <div className="daily-goal-bars">
              <div className="daily-goal-item">
                <div className="daily-goal-meta">
                  <span>{t("Từ đã ôn hôm nay", "Words reviewed today")}</span>
                  <strong>
                    {todayStats.reviewedToday} / {DAILY_WORDS_TARGET} {t("từ", "words")}
                  </strong>
                </div>
                <div className="daily-goal-track">
                  <span className="daily-goal-fill-words" style={{ width: `${wordsPct}%` }} />
                </div>
              </div>
              <div className="daily-goal-item">
                <div className="daily-goal-meta">
                  <span>{t("XP hôm nay", "XP earned today")}</span>
                  <strong className="daily-goal-xp-val">
                    {todayStats.xpToday} / {DAILY_XP_TARGET} XP
                  </strong>
                </div>
                <div className="daily-goal-track">
                  <span className="daily-goal-fill-xp" style={{ width: `${xpPct}%` }} />
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="dashboard-streak">
          <span className="dashboard-streak-icon">
            <Flame size={21} />
          </span>
          <span>{t("Chuỗi ngày học", "Learning streak")}</span>
          <strong>
            {activeLiveSummary?.streak?.currentDays ?? user?.profile?.streakDays ?? 0}
            <small> {t("ngày", "days")}</small>
          </strong>
          <small>
            {(activeLiveSummary?.streak?.currentDays ?? user?.profile?.streakDays ?? 0) > 0
              ? t("Giữ nhịp học hôm nay", "Keep your streak today")
              : t("Bắt đầu chuỗi học của bạn", "Start your learning streak")}
          </small>
        </div>

        <div className="dashboard-orbit" aria-hidden="true">
          <span className="orbit-ring orbit-ring-one" />
          <span className="orbit-ring orbit-ring-two" />
          <span className="orbit-leaf">
            <span>Aa</span>
            <small className="orbit-cefr-badge">EN · A1–C2</small>
          </span>
          <span className="orbit-dot" />
        </div>
      </section>

      {/* 2. STATS BAR */}
      <section className="dashboard-stats" aria-label={t("Tiến độ học tập", "Learning progress")}>
        <article className="dashboard-stat-xp">
          <span className="dashboard-stat-icon">
            <Zap size={17} />
          </span>
          <div>
            <small>{t("XP tích lũy", "XP earned")}</small>
            <strong>
              {(activeLiveSummary?.totalXp ?? user?.profile?.experiencePoints ?? 0).toLocaleString(locale)}
            </strong>
          </div>
          <span className="dashboard-stat-note">{t("Hồ sơ", "Profile")}</span>
        </article>
        <article>
          <span className="dashboard-stat-icon">
            <NotebookTabs size={17} />
          </span>
          <div>
            <small>{t("Quiz đã hoàn thành", "Quizzes completed")}</small>
            <strong>{sessions.length}</strong>
          </div>
          <span className="dashboard-stat-note">{t("Thiết bị này", "This device")}</span>
        </article>
        <article>
          <span className="dashboard-stat-icon">
            <RotateCcw size={17} />
          </span>
          <div>
            <small>{t("Quiz tuần này", "Quizzes this week")}</small>
            <strong>{weekly.length}</strong>
          </div>
          <span className="dashboard-stat-note">{t("7 ngày qua", "Last 7 days")}</span>
        </article>
        <article>
          <span className="dashboard-stat-icon">
            <BookOpen size={17} />
          </span>
          <div>
            <small>{t("Kho từ vựng", "Vocabulary library")}</small>
            <strong>
              {effectiveVocabCount === null ? "—" : effectiveVocabCount.toLocaleString(locale)}
            </strong>
          </div>
          <span className="dashboard-stat-note">{t("Có thể học", "Ready to learn")}</span>
        </article>
      </section>

      {/* 3. WORD OF THE DAY INTERACTIVE CARD (G2 - Real /api/vocabulary) */}
      {wordOfTheDay && (
        <section className="wotd-card" aria-label={t("Từ vựng hôm nay", "Word of the Day")}>
          <div className="wotd-left">
            <div className="wotd-top-row">
              <span className="wotd-eyebrow">
                <Sparkles size={13} aria-hidden="true" /> {t("TỪ VỰNG HÔM NAY", "WORD OF THE DAY")}
              </span>
              <div className="wotd-badges">
                {wordOfTheDay.cefr_level && (
                  <span className="study-level">{wordOfTheDay.cefr_level}</span>
                )}
                {wordOfTheDay.part_of_speech && (
                  <span className="study-pos">{wordOfTheDay.part_of_speech}</span>
                )}
              </div>
            </div>

            <div className="wotd-term-row">
              <h2>{wordOfTheDay.term}</h2>
              {wordOfTheDay.ipa && <span className="wotd-ipa">{wordOfTheDay.ipa}</span>}
              <button
                type="button"
                onClick={pronounceWotd}
                className={`wotd-audio-btn ${isSpeakingWotd ? "is-speaking" : ""}`}
                aria-label={`${t("Nghe phát âm", "Listen")} ${wordOfTheDay.term}`}
              >
                <Volume2 size={15} aria-hidden="true" />
                <span>{t("Nghe phát âm", "Listen")}</span>
              </button>
            </div>

            <p className="wotd-meaning">{wordOfTheDay.meaning}</p>

            {wordOfTheDay.examples?.[0]?.source_text && (
              <div className="wotd-example">
                <p lang="en">&ldquo;{wordOfTheDay.examples[0].source_text}&rdquo;</p>
                {wordOfTheDay.examples[0].target_text && (
                  <small>{wordOfTheDay.examples[0].target_text}</small>
                )}
              </div>
            )}
          </div>

          <div className="wotd-actions">
            <Link href="/learn" className="dashboard-primary wotd-study-btn">
              {t("Học ngay", "Study now")} <ArrowRight size={15} />
            </Link>
            <button
              type="button"
              className="wotd-shuffle-btn"
              onClick={() => setWotdOffset((n) => n + 1)}
            >
              <RefreshCw size={14} aria-hidden="true" /> {t("Đổi từ khác", "Another word")}
            </button>
          </div>
        </section>
      )}

      {/* 4. COMMUNITY LEADERBOARD CARDS (100% REAL API DATA, NO MOCK) */}
      <section className="dashboard-community-grid" aria-label={t("Bảng xếp hạng cộng đồng", "Community leaderboard")}>
        <article className="dashboard-panel dashboard-ranking-card">
          <div className="dashboard-section-heading">
            <div>
              <p className="dashboard-eyebrow">{t("BXH CÁ NHÂN", "PERSONAL RANKING")}</p>
              <h2>
                {currentEntry
                  ? `${t("Hạng", "Rank")} #${currentEntry.rank}`
                  : t("Thứ hạng của bạn", "Your ranking")}
              </h2>
            </div>
            <Trophy size={18} />
          </div>
          <div className="dashboard-ranking-highlight">
            <strong>{currentEntry ? currentEntry.xp.toLocaleString(locale) : "—"}</strong>
            <span>{t("XP tích lũy", "Total XP")}</span>
          </div>
          {leaderboard.length > 0 ? (
            <div className="dashboard-mini-ranking">
              {leaderboard.slice(0, 3).map((entry) => (
                <div key={entry.user_id}>
                  <b>#{entry.rank}</b>
                  <span>{entry.name}</span>
                  <strong>{entry.xp.toLocaleString(locale)} XP</strong>
                </div>
              ))}
            </div>
          ) : (
            <p className="dashboard-empty-rank">
              {t(
                "Hoàn thành bài học hoặc mini-game đầu tiên để lên bảng xếp hạng.",
                "Complete your first lesson or mini-game to appear on the leaderboard."
              )}
            </p>
          )}
          <Link href="/leaderboard" className="dashboard-panel-link">
            {t("Xem BXH cá nhân", "View personal ranking")} <ArrowRight size={14} />
          </Link>
        </article>

        <article className="dashboard-panel dashboard-ranking-card">
          <div className="dashboard-section-heading">
            <div>
              <p className="dashboard-eyebrow">{t("BXH CLAN", "CLAN RANKING")}</p>
              <h2>{t("Clan hiện tại", "Current clan")}</h2>
            </div>
            <Users size={18} />
          </div>
          <div className="dashboard-clan-score">
            <span>
              <Users size={16} /> {leaderboard.length} {t("thành viên", "members")}
            </span>
            <strong>
              {leaderboard.reduce((sum, entry) => sum + entry.xp, 0).toLocaleString(locale)} XP
            </strong>
          </div>
          {leaderboard.length > 0 ? (
            <div className="dashboard-mini-ranking">
              {leaderboard.slice(0, 3).map((entry) => (
                <div key={entry.user_id}>
                  <b>{entry.rank === 1 ? "🏆" : `#${entry.rank}`}</b>
                  <span>{entry.name}</span>
                  <strong>
                    {entry.words_learned ?? "—"} {t("từ", "words")}
                  </strong>
                </div>
              ))}
            </div>
          ) : (
            <p className="dashboard-empty-rank">
              {t(
                "Chưa có dữ liệu bảng xếp hạng Clan. Bắt đầu học để ghi điểm đầu tiên!",
                "No Clan ranking data yet. Start learning to score the first points!"
              )}
            </p>
          )}
          <Link href="/leaderboard" className="dashboard-panel-link">
            {t("Xem BXH Clan", "View clan ranking")} <ArrowRight size={14} />
          </Link>
        </article>
      </section>

      {/* 5. MAIN 6-CARD ACTIVITY GRID (3x2 DESKTOP) + RIGHT ACTIVITY & MILESTONES */}
      <div className="dashboard-content-grid">
        <section className="dashboard-section dashboard-activities">
          <div className="dashboard-section-heading">
            <div>
              <p className="dashboard-eyebrow">{t("CHỌN CÁCH HỌC", "CHOOSE HOW TO LEARN")}</p>
              <h2>{t("Tiếp tục hành trình", "Continue your journey")}</h2>
            </div>
            <span className="dashboard-section-caption">
              {t("6 hoạt động sẵn sàng", "6 ready activities")}
            </span>
          </div>

          <div className="dashboard-activity-grid dashboard-activity-grid-6">
            <Link href="/words" className="dashboard-activity-card activity-vocab">
              <span className="activity-icon">
                <BookOpen />
              </span>
              <span className="activity-copy">
                <strong>{t("Học từ mới", "Learn new words")}</strong>
                <small>
                  {t(
                    "Khám phá từ vựng CEFR theo chủ đề và ví dụ ngữ cảnh.",
                    "Explore CEFR vocabulary by topic with examples."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {effectiveVocabCount === null
                  ? t("Mở thư viện", "Open library")
                  : `${effectiveVocabCount.toLocaleString(locale)} ${t("từ vựng", "words")}`}
                <ArrowRight size={15} />
              </span>
            </Link>

            <Link href="/learn" className="dashboard-activity-card activity-review">
              <span className="activity-icon">
                <RotateCcw />
              </span>
              <span className="activity-copy">
                <strong>{t("Ôn flashcard", "Review flashcards")}</strong>
                <small>
                  {t(
                    "Gợi nhớ chủ động với nhịp lặp lại ngắt quãng SM-2.",
                    "Build active recall with SM-2 spaced repetition."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {t("Bắt đầu phiên ôn", "Start review session")} <ArrowRight size={15} />
              </span>
            </Link>

            <Link href="/quiz" className="dashboard-activity-card activity-quiz">
              <span className="activity-icon">
                <Brain />
              </span>
              <span className="activity-copy">
                <strong>{t("Quiz nhanh", "Quick quiz")}</strong>
                <small>
                  {t(
                    "Kiểm tra vốn từ và nhận giải thích ngay lập tức.",
                    "Test your vocabulary and get instant explanations."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {t("10 câu · khoảng 5 phút", "10 questions · ~5 min")} <ArrowRight size={15} />
              </span>
            </Link>

            <Link href="/games" className="dashboard-activity-card activity-games">
              <span className="activity-icon">
                <Gamepad2 />
              </span>
              <span className="activity-copy">
                <strong>{t("Game Center", "Game Center")}</strong>
                <small>
                  {t(
                    "6 mini-game phản xạ từ vựng, nghe âm và xếp câu.",
                    "6 interactive mini-games for reflexes, audio & sentences."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {t("Chơi ngay", "Play now")} <ArrowRight size={15} />
              </span>
            </Link>

            <Link href="/tutor" className="dashboard-activity-card activity-tutor">
              <span className="activity-icon">
                <Bot />
              </span>
              <span className="activity-copy">
                <strong>{t("LingLing AI", "LingLing AI")}</strong>
                <small>
                  {t(
                    "Đóng vai hội thoại thực tế và sửa lỗi ngữ pháp 24/7.",
                    "Practice real-life roleplay & instant grammar coaching."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {t("Trò chuyện ngay", "Chat now")} <ArrowRight size={15} />
              </span>
            </Link>

            <Link href="/arena" className="dashboard-activity-card activity-arena">
              <span className="activity-icon">
                <Swords />
              </span>
              <span className="activity-copy">
                <strong>{t("Đấu trường 1vs1", "1v1 Arena")}</strong>
                <small>
                  {t(
                    "Đấu phản xạ từ vựng tốc độ cao và phân tích lỗi sai.",
                    "Compete in fast-paced vocabulary duels with AI tips."
                  )}
                </small>
              </span>
              <span className="activity-meta">
                {t("Vào đấu trường", "Enter arena")} <ArrowRight size={15} />
              </span>
            </Link>
          </div>
        </section>

        <aside className="dashboard-side-column">
          <section className="dashboard-panel dashboard-weekly">
            <div className="dashboard-section-heading">
              <div>
                <p className="dashboard-eyebrow">{t("TỪ VỰNG ĐÃ HỌC", "WORDS LEARNED")}</p>
                <h2>{t(`${activityRange} ngày gần đây`, `Last ${activityRange} days`)}</h2>
              </div>
              <div
                className="activity-range-control"
                role="group"
                aria-label={t("Khoảng thời gian hoạt động", "Activity time range")}
              >
                <button
                  type="button"
                  onClick={() => setActivityRange(7)}
                  aria-pressed={activityRange === 7}
                >
                  {t("7 ngày", "7 days")}
                </button>
                <button
                  type="button"
                  onClick={() => setActivityRange(30)}
                  aria-pressed={activityRange === 30}
                >
                  {t("30 ngày", "30 days")}
                </button>
              </div>
              <span className="weekly-xp">
                <Zap size={14} />
                {rangeXp} XP
              </span>
            </div>

            {totalRangeWords === 0 ? (
              <div className="weekly-empty-state">
                <p>
                  {t(
                    "Chưa có dữ liệu học tập trong khoảng thời gian này. Hãy bắt đầu bài quiz đầu tiên để ghi nhận tiến độ!",
                    "No study activity recorded in this window yet. Take your first quiz to start drawing your progress chart!"
                  )}
                </p>
                <Link href="/quiz" className="weekly-empty-cta">
                  {t("Làm quiz đầu tiên để vẽ biểu đồ", "Take your first quiz to draw chart")}{" "}
                  <ArrowRight size={14} />
                </Link>
              </div>
            ) : (
              <div
                className={`weekly-chart ${activityRange === 30 ? "is-monthly" : ""}`}
                role="img"
                aria-label={t(
                  `Từ vựng đã học trong ${activityRange} ngày`,
                  `Words learned over the last ${activityRange} days`
                )}
              >
                {activityDays.map(({ label, count, date }) => (
                  <div className="weekly-day" key={date.toISOString()}>
                    <span className="weekly-bar-wrap">
                      <span
                        className={count ? "weekly-bar is-active" : "weekly-bar"}
                        style={{ height: `${count ? Math.min(100, 18 + count * 9) : 8}%` }}
                        title={t(`${count} từ đã học`, `${count} words learned`)}
                      />
                    </span>
                    <small suppressHydrationWarning>{label}</small>
                  </div>
                ))}
              </div>
            )}

            <p className="dashboard-footnote">
              {t(
                "Đồng bộ từ hoạt động học tập và bài quiz đã hoàn thành.",
                "Synced from your study activity and completed quizzes."
              )}
            </p>
          </section>

          <section className="dashboard-panel dashboard-achievements">
            <div className="dashboard-section-heading">
              <div>
                <p className="dashboard-eyebrow">{t("CỘT MỐC", "MILESTONES")}</p>
                <h2>{t("Huy hiệu danh dự", "Honor badges")}</h2>
              </div>
              <Trophy size={18} />
            </div>
            <div className="achievement-summary">
              <span className="achievement-mark">
                <Trophy size={17} />
              </span>
              <div>
                <strong>{t(`${unlockedCount} huy hiệu đã mở`, `${unlockedCount} unlocked`)}</strong>
                <small>
                  {t(
                    "Cột mốc được tính từ bài ôn tập và quiz đã hoàn thành",
                    "Milestones earned from completed reviews and quizzes"
                  )}
                </small>
              </div>
            </div>
            <div className="achievement-badges">
              <span className={learnedQuizzes > 0 || activeReviewStats.totalLearned > 0 ? "is-unlocked" : ""}>
                🌱 {t("Bắt đầu", "First step")}
              </span>
              <span
                className={
                  sessions.some((item) => item.totalQuestions > 0 && item.correctAnswers === item.totalQuestions)
                    ? "is-unlocked"
                    : ""
                }
              >
                🏆 {t("Hoàn hảo", "Perfect score")}
              </span>
              <span className={learnedQuizzes >= 5 ? "is-unlocked" : ""}>
                🔥 {t("Bền bỉ", "Dedicated")}
              </span>
              <span className={activeReviewStats.totalLearned >= 10 ? "is-unlocked" : ""}>
                📚 {t("Nhà sưu tầm", "Collector")}
              </span>
            </div>
          </section>
        </aside>
      </div>

      {/* 6. BOTTOM CTA */}
      <section className="dashboard-bottom-cta">
        <div>
          <span className="dashboard-eyebrow">{t("HỌC CÙNG CỘNG ĐỒNG", "LEARN TOGETHER")}</span>
          <h2>{t("Tiến lên trên bảng xếp hạng tuần", "Climb the weekly leaderboard")}</h2>
          <p>
            {t(
              "Điểm XP và thứ hạng lấy từ bảng xếp hạng của Clan.",
              "XP and rankings are synced from your Clan leaderboard."
            )}
          </p>
        </div>
        <Link href="/leaderboard" className="dashboard-secondary">
          {t("Xem giải đấu", "View leaderboard")} <ArrowRight size={16} />
        </Link>
      </section>
    </main>
  );
}
