"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowRight, BookOpen, Brain, Flame, Headphones, NotebookTabs, RotateCcw, Sparkles, Trophy, Users, Zap } from "lucide-react";
import { useAuth } from "@/components/auth/useAuth";
import { readQuizProgress, type QuizProgressRecord } from "@/lib/quiz-progress";
import LinguaMascot from "@/components/LinguaMascot";
import { MOCK_LEADERBOARD } from "@/lib/mock-data";

type VocabularyResponse = { words?: { id: string }[] };
type LeaderboardEntry = { rank: number; user_id: string; name: string; xp: number; level?: string; words_learned?: number };
const readLeaderboard = (payload: unknown): LeaderboardEntry[] => {
  const value = payload && typeof payload === "object" && "entries" in payload ? (payload as { entries: unknown }).entries : payload;
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is LeaderboardEntry => Boolean(item && typeof item === "object" && typeof (item as LeaderboardEntry).name === "string" && Number.isFinite((item as LeaderboardEntry).xp)))
    .sort((a, b) => b.xp - a.xp).map((item, index) => ({ ...item, rank: index + 1 }));
};

export default function HomePage() {
  const { user } = useAuth();
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
    return { label: activityRange === 7 ? new Intl.DateTimeFormat("vi-VN", { weekday: "short" }).format(date) : String(date.getDate()), count: daySessions.length, date };
  }), [sessions, activityRange]);
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
      <div className="dashboard-welcome"><p className="dashboard-eyebrow"><Sparkles size={14}/> KHÔNG GIAN HỌC CỦA BẠN</p><h1>{user?.displayName ? `Chào ${user.displayName.split(" ")[0]},` : "Chào bạn,"}<br/><em>sẵn sàng học chưa?</em></h1><p>Mỗi phiên học nhỏ đưa bạn đến gần hơn với sự tự tin khi dùng tiếng Anh.</p><div className="dashboard-hero-actions"><Link className="dashboard-primary" href="/learn">Bắt đầu ôn tập <ArrowRight size={16}/></Link><Link className="dashboard-text-link" href="/quiz">Làm quiz <ArrowRight size={15}/></Link></div></div>
      <div className="dashboard-streak"><span className="dashboard-streak-icon"><Flame size={21}/></span><span>Chuỗi ngày học</span><strong>{user?.profile?.streakDays ?? 0}<small> ngày</small></strong><small>{(user?.profile?.streakDays ?? 0) > 0 ? "Giữ nhịp học hôm nay" : "Bắt đầu chuỗi học của bạn"}</small></div>
      <div className="dashboard-orbit" aria-hidden="true"><span className="orbit-ring orbit-ring-one"/><span className="orbit-ring orbit-ring-two"/><span className="orbit-leaf">語</span><span className="orbit-dot"/></div><div className="dashboard-mascot"><LinguaMascot mood={sessions.length ? "correct" : "idle"} compact /></div>
    </section>

    <section className="dashboard-stats" aria-label="Tiến độ học tập">
      <article><span className="dashboard-stat-icon"><Zap size={17}/></span><div><small>XP tích lũy</small><strong>{(user?.profile?.experiencePoints ?? 0).toLocaleString("vi-VN")}</strong></div><span className="dashboard-stat-note">Hồ sơ</span></article>
      <article><span className="dashboard-stat-icon"><NotebookTabs size={17}/></span><div><small>Quiz đã hoàn thành</small><strong>{sessions.length}</strong></div><span className="dashboard-stat-note">Thiết bị này</span></article>
      <article><span className="dashboard-stat-icon"><RotateCcw size={17}/></span><div><small>Quiz tuần này</small><strong>{weekly.length}</strong></div><span className="dashboard-stat-note">7 ngày qua</span></article>
      <article><span className="dashboard-stat-icon"><BookOpen size={17}/></span><div><small>Từ đã tải</small><strong>{vocabularyCount === null ? "—" : vocabularyCount}</strong></div><span className="dashboard-stat-note">Có thể học</span></article>
    </section>

    <section className="dashboard-community-grid" aria-label="Bảng xếp hạng cộng đồng">
      <article className="dashboard-panel dashboard-ranking-card"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">BXH CÁ NHÂN</p><h2>{currentEntry ? `Hạng #${currentEntry.rank}` : "Thứ hạng của bạn"}</h2></div><Trophy size={18}/></div><div className="dashboard-ranking-highlight"><strong>{currentEntry ? currentEntry.xp.toLocaleString("vi-VN") : "—"}</strong><span>XP tuần này</span></div><div className="dashboard-mini-ranking">{leaderboard.slice(0, 3).map((entry) => <div key={entry.user_id}><b>#{entry.rank}</b><span>{entry.name}</span><strong>{entry.xp.toLocaleString("vi-VN")} XP</strong></div>)}</div><Link href="/leaderboard" className="dashboard-panel-link">Xem BXH cá nhân <ArrowRight size={14}/></Link></article>
      <article className="dashboard-panel dashboard-ranking-card"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">BXH CLAN</p><h2>Clan hiện tại</h2></div><Users size={18}/></div><div className="dashboard-clan-score"><span><Users size={16}/> {leaderboard.length} thành viên</span><strong>{leaderboard.reduce((sum, entry) => sum + entry.xp, 0).toLocaleString("vi-VN")} XP</strong></div><div className="dashboard-mini-ranking">{leaderboard.slice(0, 3).map((entry) => <div key={entry.user_id}><b>{entry.rank === 1 ? "🏆" : `#${entry.rank}`}</b><span>{entry.name}</span><strong>{entry.words_learned ?? "—"} từ</strong></div>)}</div><Link href="/leaderboard" className="dashboard-panel-link">Xem BXH Clan <ArrowRight size={14}/></Link></article>
    </section>
    <div className="dashboard-content-grid">
      <section className="dashboard-section dashboard-activities"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">CHỌN CÁCH HỌC</p><h2>Tiếp tục hành trình</h2></div><span className="dashboard-section-caption">Các hoạt động hiện có</span></div>
        <div className="dashboard-activity-grid">
          <Link href="/words" className="dashboard-activity-card activity-vocab"><span className="activity-icon"><BookOpen/></span><span className="activity-copy"><strong>Học từ mới</strong><small>Khám phá từ vựng với thẻ học và ví dụ.</small></span><span className="activity-meta">{vocabularyCount === null ? "Mở thư viện" : `${vocabularyCount}${vocabularyCount === 500 ? "+" : ""} từ đã tải`}<ArrowRight size={15}/></span></Link>
          <Link href="/learn" className="dashboard-activity-card activity-review"><span className="activity-icon"><RotateCcw/></span><span className="activity-copy"><strong>Ôn flashcard</strong><small>Gợi nhớ chủ động với nhịp lặp lại ngắt quãng.</small></span><span className="activity-meta">Bắt đầu phiên ôn <ArrowRight size={15}/></span></Link>
          <Link href="/quiz" className="dashboard-activity-card activity-quiz"><span className="activity-icon"><Brain/></span><span className="activity-copy"><strong>Quiz nhanh</strong><small>Kiểm tra vốn từ và nhận phản hồi ngay.</small></span><span className="activity-meta">10 câu · khoảng 5 phút <ArrowRight size={15}/></span></Link>
          <div className="dashboard-activity-card activity-planned" aria-label="Listening practice, tính năng dự kiến"><span className="activity-icon"><Headphones/></span><span className="activity-copy"><strong>Luyện nghe</strong><small>Rèn khả năng nhận biết từ trong hội thoại.</small></span><span className="activity-meta">Tính năng dự kiến</span></div>
        </div>
      </section>
      <aside className="dashboard-side-column">
        <section className="dashboard-panel dashboard-weekly"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">TỪ VỰNG ĐÃ HỌC</p><h2>{activityRange} ngày gần đây</h2></div><div className="activity-range-control" role="group" aria-label="Khoảng thời gian hoạt động"><button onClick={() => setActivityRange(7)} aria-pressed={activityRange === 7}>7 ngày</button><button onClick={() => setActivityRange(30)} aria-pressed={activityRange === 30}>30 ngày</button></div><span className="weekly-xp"><Zap size={14}/>{rangeXp} XP</span></div><div className={`weekly-chart ${activityRange === 30 ? "is-monthly" : ""}`} role="img" aria-label={`Từ vựng đã học trong ${activityRange} ngày`}>
          {dailyVocabulary.map(({ label, count, date }) => <div className="weekly-day" key={date.toISOString()}><span className="weekly-bar-wrap"><span className={count ? "weekly-bar is-active" : "weekly-bar"} style={{ height: `${count ? Math.min(100, 18 + count * 9) : 8}%` }} title={`${count} từ đã học`}/></span><small>{label}</small></div>)}
        </div><p className="dashboard-footnote">Dựa trên quiz đã lưu trên thiết bị này.</p></section>
        <section className="dashboard-panel dashboard-achievements"><div className="dashboard-section-heading"><div><p className="dashboard-eyebrow">CỘT MỐC</p><h2>Huy hiệu danh dự</h2></div><Trophy size={18}/></div><div className="achievement-summary"><span className="achievement-mark"><Trophy size={17}/></span><div><strong>{unlockedCount} huy hiệu đã mở</strong><small>Cột mốc được tính từ quiz đã hoàn thành</small></div></div><div className="achievement-badges"><span className={learnedQuizzes > 0 ? "is-unlocked" : ""}>🌱 Bắt đầu</span><span className={sessions.some((item) => item.totalQuestions > 0 && item.correctAnswers === item.totalQuestions) ? "is-unlocked" : ""}>🏆 Hoàn hảo</span><span className={learnedQuizzes >= 5 ? "is-unlocked" : ""}>🔥 Bền bỉ</span><span className={vocabularyCount !== null && vocabularyCount >= 100 ? "is-unlocked" : ""}>📚 Nhà sưu tầm</span></div></section>
      </aside>
    </div>
    <section className="dashboard-bottom-cta"><div><span className="dashboard-eyebrow">HỌC CÙNG CỘNG ĐỒNG</span><h2>Tiến lên trên bảng xếp hạng tuần</h2><p>Điểm XP và thứ hạng lấy từ bảng xếp hạng của Clan.</p></div><Link href="/leaderboard" className="dashboard-secondary">Xem giải đấu <ArrowRight size={16}/></Link></section>
  </main>;
}
