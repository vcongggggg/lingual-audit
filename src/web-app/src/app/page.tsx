"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowRight, BookOpen, Brain, Flame, Headphones, NotebookTabs, RotateCcw, Sparkles, Trophy, Users, Zap } from "lucide-react";
import { useAuth } from "@/components/auth/useAuth";
import { readQuizProgress, type QuizProgressRecord } from "@/lib/quiz-progress";
import LinguaMascot from "@/components/LinguaMascot";
import { MOCK_LEADERBOARD } from "@/lib/mock-data";
import { useLanguage } from "@/lib/language-context";

type VocabularyResponse = { words?: { id: string }[] };
const VI_WEEKDAYS = ["CN", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7"];
type LeaderboardEntry = { rank: number; user_id: string; name: string; xp: number; level?: string; words_learned?: number };
const readLeaderboard = (payload: unknown): LeaderboardEntry[] => {
  const value = payload && typeof payload === "object" && "entries" in payload ? (payload as { entries: unknown }).entries : payload;
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is LeaderboardEntry => Boolean(item && typeof item === "object" && typeof (item as LeaderboardEntry).name === "string" && Number.isFinite((item as LeaderboardEntry).xp)))
    .sort((a, b) => b.xp - a.xp).map((item, index) => ({ ...item, rank: index + 1 }));
};

export default function HomePage() {
  const { user } = useAuth();
  const { translate: t, language } = useLanguage();
  const [sessions, setSessions] = useState<QuizProgressRecord[]>([]);
  const [vocabularyCount, setVocabularyCount] = useState<number | null>(null);
  const [activityRange, setActivityRange] = useState<7 | 30>(7);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>(MOCK_LEADERBOARD);
  const refresh = useCallback(() => setSessions(readQuizProgress()), []);
  useEffect(() => {
    const initialRefresh = window.setTimeout(refresh, 0);
    window.addEventListener("lingual:quiz-progress", refresh);
    const controller = new AbortController();
    void fetch("/api/vocabulary?limit=500", { signal: controller.signal, cache: "no-store" })
      .then((response) => response.ok ? response.json() as Promise<VocabularyResponse> : null)
      .then((data) => { if (data && Array.isArray(data.words)) setVocabularyCount(data.words.length); })
      .catch(() => {});
    void fetch("/api/leaderboard", { cache: "no-store", signal: controller.signal })
      .then((response) => response.ok ? response.json() : null)
      .then((data) => { const entries = readLeaderboard(data); if (entries.length) setLeaderboard(entries); })
      .catch(() => {});
    return () => { window.clearTimeout(initialRefresh); window.removeEventListener("lingual:quiz-progress", refresh); controller.abort(); };
  }, [refresh]);

  const weekly = useMemo(() => {
    const start = new Date(); start.setHours(0, 0, 0, 0); start.setDate(start.getDate() - 6);
    return sessions.filter((session) => new Date(session.completedAt) >= start);
  }, [sessions]);
  const activityDays = useMemo(() => Array.from({ length: activityRange }, (_, offset) => {
    const date = new Date(); date.setHours(0, 0, 0, 0); date.setDate(date.getDate() - (activityRange - 1 - offset));
    const daySessions = sessions.filter((session) => new Date(session.completedAt).toDateString() === date.toDateString());
    return { label: activityRange === 7 ? new Intl.DateTimeFormat(language === "vi" ? "vi-VN" : "en-US", { weekday: "short" }).format(date) : String(date.getDate()), count: daySessions.length, date };
  }), [sessions, activityRange, language]);
  const rangeXp = sessions.filter((session) => {
    const start = new Date(); start.setHours(0, 0, 0, 0); start.setDate(start.getDate() - (activityRange - 1));
    return new Date(session.completedAt) >= start;
  }).reduce((total, item) => total + item.xp, 0);
  const learnedQuizzes = sessions.length;
  const unlockedCount = Number(learnedQuizzes > 0) + Number(sessions.some((item) => item.totalQuestions > 0 && item.correctAnswers === item.totalQuestions)) + Number(learnedQuizzes >= 5) + Number(vocabularyCount !== null && vocabularyCount >= 100);
  const dailyVocabulary = useMemo(() => activityDays.map((day) => ({
    ...day,
    count: sessions.filter((session) => new Date(session.completedAt).toDateString() === day.date.toDateString())
      .reduce((total, session) => total + session.correctAnswers, 0),
  })), [activityDays, sessions]);
  const currentEntry = leaderboard.find((entry) => entry.user_id === user?.id || entry.user_id === user?.mezonUserId);

  return <main className="dashboard-page">
    <section className="dashboard-hero">
      <div className="dashboard-welcome"><p className="dashboard-eyebrow"><Sparkles size={14}/> {t("KHÔNG GIAN HỌC CỦA BẠN", "YOUR LEARNING SPACE")}</p><h1>{user?.displayName ? `${t("Chào", "Hello")} ${user.displayName.split(" ")[0]},` : t("Chào bạn,", "Hello,")}<br/><em>{t("sẵn sàng học chưa?", "ready to learn?")}</em></h1><p>{t("Mỗi phiên học nhỏ đưa bạn đến gần hơn với sự tự tin khi dùng tiếng Anh.", "Every small session brings you closer to confidence in English.")}</p><div className="dashboard-hero-actions"><Link className="dashboard-primary" href="/learn">{t("Bắt đầu ôn tập", "Start reviewing")} <ArrowRight size={16}/></Link><Link className="dashboard-text-link" href="/quiz">{t("Làm quiz", "Take a quiz")} <ArrowRight size={15}/></Link></div></div>
      <div className="dashboard-streak"><span className="dashboard-streak-icon"><Flame size={21}/></span><span>{t("Chuỗi ngày học", "Learning streak")}</span><strong>{user?.profile?.streakDays ?? 0}<small> {t("ngày", "days")}</small></strong><small>{(user?.profile?.streakDays ?? 0) > 0 ? t("Giữ nhịp học hôm nay", "Keep your streak today") : t("Bắt đầu chuỗi học của bạn", "Start your learning streak")}</small></div>
      <div className="dashboard-orbit" aria-hidden="true"><span className="orbit-ring orbit-ring-one"/><span className="orbit-ring orbit-ring-two"/><span className="orbit-leaf">語</span><span className="orbit-dot"/></div><div className="dashboard-mascot"><LinguaMascot mood={sessions.length ? "correct" : "idle"} compact /></div>
    </section>

    <section className="dashboard-stats" aria-label={t("Tiến độ học tập", "Learning progress")}>
      <article><span className="dashboard-stat-icon"><Zap size={17}/></span><div><small>{t("XP tích lũy", "XP earned")}</small><strong>{(user?.profile?.experiencePoints ?? 0).toLocaleString(language === "vi" ? "vi-VN" : "en-US")}</strong></div><span className="dashboard-stat-note">{t("Hồ sơ", "Profile")}</span></article>
      <article><span className="dashboard-stat-icon"><NotebookTabs size={17}/></span><div><small>{t("Quiz đã hoàn thành", "Quizzes completed")}</small><strong>{sessions.length}</strong></div><span className="dashboard-stat-note">{t("Thiết bị này", "This device")}</span></article>
      <article><span className="dashboard-stat-icon"><RotateCcw size={17}/></span><div><small>{t("Quiz tuần này", "Quizzes this week")}</small><strong>{weekly.length}</strong></div><span className="dashboard-stat-note">{t("7 ngày qua", "Last 7 days")}</span></article>
      <article><span className="dashboard-stat-icon"><BookOpen size={17}/></span><div><small>{t("Từ đã tải", "Words loaded")}</small><strong>{vocabularyCount === null ? "—" : vocabularyCount}</strong></div><span className="dashboard-stat-note">{t("Có thể học", "Ready to learn")}</span></article>
    </section>

    <section className="dashboard-community-grid" aria-label={t("Bảng xếp hạng cộng đồng", "Community leaderboard")}>
      <article className="dashboard-panel dashboard-ranking-card"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">{t("BXH CÁ NHÂN", "PERSONAL RANKING")}</p><h2>{currentEntry ? `${t("Hạng", "Rank")} #${currentEntry.rank}` : t("Thứ hạng của bạn", "Your ranking")}</h2></div><Trophy size={18}/></div><div className="dashboard-ranking-highlight"><strong>{currentEntry ? currentEntry.xp.toLocaleString(language === "vi" ? "vi-VN" : "en-US") : "—"}</strong><span>{t("XP tuần này", "XP this week")}</span></div><div className="dashboard-mini-ranking">{leaderboard.slice(0, 3).map((entry) => <div key={entry.user_id}><b>#{entry.rank}</b><span>{entry.name}</span><strong>{entry.xp.toLocaleString(language === "vi" ? "vi-VN" : "en-US")} XP</strong></div>)}</div><Link href="/leaderboard" className="dashboard-panel-link">{t("Xem BXH cá nhân", "View personal ranking")} <ArrowRight size={14}/></Link></article>
      <article className="dashboard-panel dashboard-ranking-card"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">{t("BXH CLAN", "CLAN RANKING")}</p><h2>{t("Clan hiện tại", "Current clan")}</h2></div><Users size={18}/></div><div className="dashboard-clan-score"><span><Users size={16}/> {leaderboard.length} {t("thành viên", "members")}</span><strong>{leaderboard.reduce((sum, entry) => sum + entry.xp, 0).toLocaleString(language === "vi" ? "vi-VN" : "en-US")} XP</strong></div><div className="dashboard-mini-ranking">{leaderboard.slice(0, 3).map((entry) => <div key={entry.user_id}><b>{entry.rank === 1 ? "🏆" : `#${entry.rank}`}</b><span>{entry.name}</span><strong>{entry.words_learned ?? "—"} {t("từ", "words")}</strong></div>)}</div><Link href="/leaderboard" className="dashboard-panel-link">{t("Xem BXH Clan", "View clan ranking")} <ArrowRight size={14}/></Link></article>
    </section>
    <div className="dashboard-content-grid">
      <section className="dashboard-section dashboard-activities"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">{t("CHỌN CÁCH HỌC", "CHOOSE HOW TO LEARN")}</p><h2>{t("Tiếp tục hành trình", "Continue your journey")}</h2></div><span className="dashboard-section-caption">{t("Các hoạt động hiện có", "Available activities")}</span></div>
        <div className="dashboard-activity-grid">
          <Link href="/words" className="dashboard-activity-card activity-vocab"><span className="activity-icon"><BookOpen/></span><span className="activity-copy"><strong>{t("Học từ mới", "Learn new words")}</strong><small>{t("Khám phá từ vựng với thẻ học và ví dụ.", "Explore vocabulary with flashcards and examples.")}</small></span><span className="activity-meta">{vocabularyCount === null ? t("Mở thư viện", "Open library") : `${vocabularyCount}${vocabularyCount === 500 ? "+" : ""} ${t("từ đã tải", "words loaded")}`}<ArrowRight size={15}/></span></Link>
          <Link href="/learn" className="dashboard-activity-card activity-review"><span className="activity-icon"><RotateCcw/></span><span className="activity-copy"><strong>{t("Ôn flashcard", "Review flashcards")}</strong><small>{t("Gợi nhớ chủ động với nhịp lặp lại ngắt quãng.", "Build active recall with spaced repetition.")}</small></span><span className="activity-meta">{t("Bắt đầu phiên ôn", "Start review session")} <ArrowRight size={15}/></span></Link>
          <Link href="/quiz" className="dashboard-activity-card activity-quiz"><span className="activity-icon"><Brain/></span><span className="activity-copy"><strong>{t("Quiz nhanh", "Quick quiz")}</strong><small>{t("Kiểm tra vốn từ và nhận phản hồi ngay.", "Test your vocabulary and get instant feedback.")}</small></span><span className="activity-meta">{t("10 câu · khoảng 5 phút", "10 questions · about 5 minutes")} <ArrowRight size={15}/></span></Link>
          <div className="dashboard-activity-card activity-planned" aria-label={t("Luyện nghe, tính năng dự kiến", "Listening practice, planned feature")}><span className="activity-icon"><Headphones/></span><span className="activity-copy"><strong>{t("Luyện nghe", "Listening practice")}</strong><small>{t("Rèn khả năng nhận biết từ trong hội thoại.", "Train your ability to recognize words in conversation.")}</small></span><span className="activity-meta">{t("Tính năng dự kiến", "Planned feature")}</span></div>
        </div>
      </section>
      <aside className="dashboard-side-column">
        <section className="dashboard-panel dashboard-weekly"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">TỪ VỰNG ĐÃ HỌC</p><h2>{activityRange} ngày gần đây</h2></div><div className="activity-range-control" role="group" aria-label="Khoảng thời gian hoạt động"><button onClick={() => setActivityRange(7)} aria-pressed={activityRange === 7}>7 ngày</button><button onClick={() => setActivityRange(30)} aria-pressed={activityRange === 30}>30 ngày</button></div><span className="weekly-xp"><Zap size={14}/>{rangeXp} XP</span></div><div className={`weekly-chart ${activityRange === 30 ? "is-monthly" : ""}`} role="img" aria-label={`Từ vựng đã học trong ${activityRange} ngày`}>
          {dailyVocabulary.map(({ label, count, date }) => <div className="weekly-day" key={date.toISOString()}><span className="weekly-bar-wrap"><span className={count ? "weekly-bar is-active" : "weekly-bar"} style={{ height: `${count ? Math.min(100, 18 + count * 9) : 8}%` }} title={`${count} từ đã học`}/></span><small suppressHydrationWarning>{label}</small></div>)}
        </div><p className="dashboard-footnote">Dựa trên quiz đã lưu trên thiết bị này.</p></section>
        <section className="dashboard-panel dashboard-achievements"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">CỘT MỐC</p><h2>Huy hiệu danh dự</h2></div><Trophy size={18}/></div><div className="achievement-summary"><span className="achievement-mark"><Trophy size={17}/></span><div><strong>{unlockedCount} huy hiệu đã mở</strong><small>Cột mốc được tính từ quiz đã hoàn thành</small></div></div><div className="achievement-badges"><span className={learnedQuizzes > 0 ? "is-unlocked" : ""}>🌱 Bắt đầu</span><span className={sessions.some((item) => item.totalQuestions > 0 && item.correctAnswers === item.totalQuestions) ? "is-unlocked" : ""}>🏆 Hoàn hảo</span><span className={learnedQuizzes >= 5 ? "is-unlocked" : ""}>🔥 Bền bỉ</span><span className={vocabularyCount !== null && vocabularyCount >= 100 ? "is-unlocked" : ""}>📚 Nhà sưu tầm</span></div></section>
      </aside>
    </div>
    <section className="dashboard-bottom-cta"><div><span className="dashboard-eyebrow">HỌC CÙNG CỘNG ĐỒNG</span><h2>Tiến lên trên bảng xếp hạng tuần</h2><p>Điểm XP và thứ hạng lấy từ bảng xếp hạng của Clan.</p></div><Link href="/leaderboard" className="dashboard-secondary">Xem giải đấu <ArrowRight size={16}/></Link></section>
  </main>;
}
