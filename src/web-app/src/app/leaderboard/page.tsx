"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/components/auth/useAuth";
import { ArrowRight, ChevronDown, ChevronUp, Minus, Trophy, Users, Zap } from "lucide-react";
import { MOCK_LEADERBOARD } from "@/lib/mock-data";
import { useLanguage } from "@/lib/language-context";

type Entry = { rank: number; user_id: string; name: string; xp: number; avatar?: string; previous_rank?: number; level?: string; words_learned?: number };
function readEntries(payload: unknown): Entry[] | null {
  let value = payload;
  if (value && typeof value === "object" && "data" in value) value = (value as { data: unknown }).data;
  if (value && typeof value === "object" && "entries" in value) value = (value as { entries: unknown }).entries;
  if (!Array.isArray(value)) return null;
  const unique = new Map<string, Entry>();
  for (const raw of value) {
    if (!raw || typeof raw !== "object") continue;
    const item = raw as Partial<Entry>;
    if (typeof item.user_id !== "string" || !item.user_id || typeof item.name !== "string" || !Number.isFinite(item.xp)) continue;
    const entry = { ...item, rank: Number.isFinite(item.rank) ? item.rank! : 0, xp: item.xp! } as Entry;
    if (!unique.has(entry.user_id)) unique.set(entry.user_id, entry);
  }
  let rank = 0;
  let previousXp: number | undefined;
  return [...unique.values()].sort((a, b) => b.xp - a.xp || a.name.localeCompare(b.name)).map((entry, index) => {
    if (entry.xp !== previousXp) rank = index + 1;
    previousXp = entry.xp;
    return { ...entry, rank };
  });
}

const initials = (name: string) => name.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join("").toLocaleUpperCase("vi-VN") || "?";
export default function LeaderboardPage() {
  const { user } = useAuth();
  const { translate: t, language } = useLanguage();
  const [entries, setEntries] = useState<Entry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [demo, setDemo] = useState(false);
  const [pageSize, setPageSize] = useState(20);
  const load = useCallback(async () => {
    setLoading(true); setError(false);
    try {
      const response = await fetch("/api/leaderboard", { cache: "no-store", signal: AbortSignal.timeout(10000) });
      if (!response.ok) throw new Error("leaderboard unavailable");
      const valid = readEntries(await response.json());
      if (!valid) throw new Error("leaderboard payload is unavailable");
      setEntries(valid); setDemo(false);
    } catch { setEntries(MOCK_LEADERBOARD); setDemo(true); setError(false); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { const initialLoad = window.setTimeout(() => { void load(); }, 0); return () => window.clearTimeout(initialLoad); }, [load]);
  const isCurrentUser = (entry: Entry) => entry.user_id === user?.id || entry.user_id === user?.mezonUserId;
  const current = entries.find(isCurrentUser);
  const visibleEntries = entries;
  const listedEntries = visibleEntries.slice(3, pageSize);
  const rankChange = (entry: Entry) => entry.previous_rank === undefined ? null : entry.previous_rank - entry.rank;

  return <main className="leaderboard-page">
    <header className="leaderboard-heading"><div><p className="leaderboard-eyebrow">LINGUAL · {t("CỘNG ĐỒNG", "COMMUNITY")}</p><h1>{t("Giải đấu học tập tuần này", "This week's learning league")}</h1><p className="leaderboard-subtitle">{t("Cùng Clan duy trì nhịp học mỗi ngày.", "Keep your daily learning rhythm with your clan.")}</p></div><div className="leaderboard-cycle"><span>{t("CHU KỲ HIỆN TẠI", "CURRENT CYCLE")}</span><strong>{t("Tuần này", "This week")}</strong><small>{t("XP được làm mới vào đầu tuần", "XP resets at the start of the week")}</small></div></header>
    <section className="leaderboard-summary" aria-label={t("Tổng quan thứ hạng", "Ranking overview")}><div className="leaderboard-summary-icon"><Trophy size={20}/></div><div><span>{t("Thứ hạng của bạn", "Your ranking")}</span><strong>{loading ? t("Đang tải…", "Loading…") : current ? `#${current.rank}` : t("Chưa có hạng", "Unranked")}</strong></div><div className="leaderboard-summary-divider"/><div><span>{t("XP cá nhân tuần này", "Personal XP this week")}</span><strong>{current ? current.xp.toLocaleString(language === "vi" ? "vi-VN" : "en-US") : "—"} <small>XP</small></strong></div><Link href="/quiz" className="leaderboard-cta">{t("Làm Quiz", "Take quiz")} <ArrowRight size={16}/></Link></section>
    <section className="league-metrics" aria-label={t("Tổng quan giải đấu", "League overview")}><div><span>{t("Thành viên trong bảng", "Members on board")}</span><strong>{entries.length}</strong></div><div><span>{t("Tổng XP tuần", "Total weekly XP")}</span><strong>{entries.reduce((sum, entry) => sum + entry.xp, 0).toLocaleString(language === "vi" ? "vi-VN" : "en-US")} XP</strong></div><div><span>{t("XP để lên hạng kế", "XP to next rank")}</span><strong>{current && current.rank > 1 ? Math.max(0, (entries.find((entry) => entry.rank === current.rank - 1)?.xp ?? current.xp) - current.xp + 1).toLocaleString(language === "vi" ? "vi-VN" : "en-US") : current?.rank === 1 ? t("Đang dẫn đầu", "Leading") : "—"}</strong></div></section>
    <section className="leaderboard-panel"><div className="leaderboard-panel-heading"><div><p className="leaderboard-eyebrow">{t("TUẦN NÀY", "THIS WEEK")}</p><h2>{t("Học viên nổi bật", "Top learners")}</h2></div><span className="leaderboard-scope"><Users size={15}/> {t("Clan hiện tại", "Current clan")}</span></div>{demo && <p className="leaderboard-demo-note">{t("Bản demo · Dữ liệu mẫu cục bộ, không phản ánh thứ hạng thật.", "Demo · Local sample data, not a live ranking.")}</p>}
      {loading ? <div className="leaderboard-state" role="status" aria-label="Đang tải bảng xếp hạng"><span className="leaderboard-skeleton"/><span className="leaderboard-skeleton"/><span className="leaderboard-skeleton"/></div> : error ? <div className="leaderboard-state" role="alert"><p>Không thể tải bảng xếp hạng. Hãy kiểm tra kết nối rồi thử lại.</p><button className="leaderboard-secondary" onClick={() => void load()}>Thử tải lại</button></div> : visibleEntries.length === 0 ? <div className="leaderboard-state"><p>Chưa có người học trong bảng xếp hạng tuần này.</p><span>Hoàn thành một bài học để bắt đầu tích lũy XP.</span></div> : <>
        <div className="leaderboard-podium">{visibleEntries.slice(0, 3).map((entry) => { const mine = isCurrentUser(entry); return <article key={entry.user_id} className={`leaderboard-podium-card rank-${entry.rank} ${mine ? "is-current" : ""}`}><span className="leaderboard-medal" aria-label={`Hạng ${entry.rank}`}>{entry.rank}</span>{entry.avatar ? <img className="leaderboard-avatar" src={entry.avatar} alt=""/> : <span className="leaderboard-avatar" aria-label={initials(entry.name)}>{initials(entry.name)}</span>}<strong>{entry.name}{mine && <small> · Bạn</small>}</strong>{entry.level && <small className="leaderboard-level">{entry.level}</small>}<span>{entry.xp.toLocaleString("vi-VN")} XP</span></article>; })}</div>
        {visibleEntries.length > 3 && <><div className="leaderboard-list" role="list" aria-label="Các vị trí tiếp theo">{listedEntries.map((entry) => { const change = rankChange(entry); const mine = isCurrentUser(entry); return <article role="listitem" key={entry.user_id} className={`leaderboard-row ${mine ? "is-current" : ""}`}><strong className="leaderboard-rank">{entry.rank}</strong>{entry.avatar ? <img className="leaderboard-avatar" src={entry.avatar} alt=""/> : <span className="leaderboard-avatar" aria-label={initials(entry.name)}>{initials(entry.name)}</span>}<strong className="leaderboard-name">{entry.name}{mine && <small>Bạn</small>}<small className="leaderboard-level">{entry.level || "—"}</small></strong><span className="leaderboard-change" aria-label={change === null ? "Chưa có dữ liệu thay đổi hạng" : change > 0 ? `Tăng ${change} hạng` : change < 0 ? `Giảm ${Math.abs(change)} hạng` : "Không đổi"}>{change === null ? "—" : change > 0 ? <><ChevronUp size={16}/>{change}</> : change < 0 ? <><ChevronDown size={16}/>{Math.abs(change)}</> : <Minus size={15}/>}</span><strong className="leaderboard-xp"><Zap size={15}/>{entry.xp.toLocaleString("vi-VN")} XP</strong></article>; })}</div>{visibleEntries.length > pageSize && <button className="leaderboard-secondary leaderboard-load-more" onClick={() => setPageSize((size) => size + 20)}>Xem thêm vị trí</button>}</>}
      </>}
    </section>
  </main>;
}
