"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/components/auth/useAuth";
import { ArrowRight, ChevronDown, ChevronUp, Minus, Trophy, Users, Zap } from "lucide-react";
import { MOCK_LEADERBOARD } from "@/lib/mock-data";

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
    <header className="leaderboard-heading"><div><p className="leaderboard-eyebrow">LINGUAL · CỘNG ĐỒNG</p><h1>Giải đấu học tập tuần này</h1><p className="leaderboard-subtitle">Cùng Clan duy trì nhịp học mỗi ngày.</p></div><div className="leaderboard-cycle"><span>CHU KỲ HIỆN TẠI</span><strong>Tuần này</strong><small>XP được làm mới vào đầu tuần</small></div></header>
    <section className="leaderboard-summary" aria-label="Tổng quan thứ hạng"><div className="leaderboard-summary-icon"><Trophy size={20}/></div><div><span>Thứ hạng của bạn</span><strong>{loading ? "Đang tải…" : current ? `#${current.rank}` : "Chưa có hạng"}</strong></div><div className="leaderboard-summary-divider"/><div><span>XP cá nhân tuần này</span><strong>{current ? current.xp.toLocaleString("vi-VN") : "—"} <small>XP</small></strong></div><Link href="/quiz" className="leaderboard-cta">Làm Quiz <ArrowRight size={16}/></Link></section>
    <section className="league-metrics" aria-label="Tổng quan giải đấu"><div><span>Thành viên trong bảng</span><strong>{entries.length}</strong></div><div><span>Tổng XP tuần</span><strong>{entries.reduce((sum, entry) => sum + entry.xp, 0).toLocaleString("vi-VN")} XP</strong></div><div><span>XP để lên hạng kế</span><strong>{current && current.rank > 1 ? Math.max(0, (entries.find((entry) => entry.rank === current.rank - 1)?.xp ?? current.xp) - current.xp + 1).toLocaleString("vi-VN") : current?.rank === 1 ? "Đang dẫn đầu" : "—"}</strong></div></section>
    <section className="leaderboard-panel"><div className="leaderboard-panel-heading"><div><p className="leaderboard-eyebrow">TUẦN NÀY</p><h2>Học viên nổi bật</h2></div><span className="leaderboard-scope"><Users size={15}/> Clan hiện tại</span></div>{demo && <p className="leaderboard-demo-note">Bản demo · Dữ liệu mẫu cục bộ, không phản ánh thứ hạng thật.</p>}
      {loading ? <div className="leaderboard-state" role="status" aria-label="Đang tải bảng xếp hạng"><span className="leaderboard-skeleton"/><span className="leaderboard-skeleton"/><span className="leaderboard-skeleton"/></div> : error ? <div className="leaderboard-state" role="alert"><p>Không thể tải bảng xếp hạng. Hãy kiểm tra kết nối rồi thử lại.</p><button className="leaderboard-secondary" onClick={() => void load()}>Thử tải lại</button></div> : visibleEntries.length === 0 ? <div className="leaderboard-state"><p>Chưa có người học trong bảng xếp hạng tuần này.</p><span>Hoàn thành một bài học để bắt đầu tích lũy XP.</span></div> : <>
        <div className="leaderboard-podium">{visibleEntries.slice(0, 3).map((entry) => { const mine = isCurrentUser(entry); return <article key={entry.user_id} className={`leaderboard-podium-card rank-${entry.rank} ${mine ? "is-current" : ""}`}><span className="leaderboard-medal" aria-label={`Hạng ${entry.rank}`}>{entry.rank}</span>{entry.avatar ? <img className="leaderboard-avatar" src={entry.avatar} alt=""/> : <span className="leaderboard-avatar" aria-label={initials(entry.name)}>{initials(entry.name)}</span>}<strong>{entry.name}{mine && <small> · Bạn</small>}</strong>{entry.level && <small className="leaderboard-level">{entry.level}</small>}<span>{entry.xp.toLocaleString("vi-VN")} XP</span></article>; })}</div>
        {visibleEntries.length > 3 && <><div className="leaderboard-list" role="list" aria-label="Các vị trí tiếp theo">{listedEntries.map((entry) => { const change = rankChange(entry); const mine = isCurrentUser(entry); return <article role="listitem" key={entry.user_id} className={`leaderboard-row ${mine ? "is-current" : ""}`}><strong className="leaderboard-rank">{entry.rank}</strong>{entry.avatar ? <img className="leaderboard-avatar" src={entry.avatar} alt=""/> : <span className="leaderboard-avatar" aria-label={initials(entry.name)}>{initials(entry.name)}</span>}<strong className="leaderboard-name">{entry.name}{mine && <small>Bạn</small>}<small className="leaderboard-level">{entry.level || "—"}</small></strong><span className="leaderboard-change" aria-label={change === null ? "Chưa có dữ liệu thay đổi hạng" : change > 0 ? `Tăng ${change} hạng` : change < 0 ? `Giảm ${Math.abs(change)} hạng` : "Không đổi"}>{change === null ? "—" : change > 0 ? <><ChevronUp size={16}/>{change}</> : change < 0 ? <><ChevronDown size={16}/>{Math.abs(change)}</> : <Minus size={15}/>}</span><strong className="leaderboard-xp"><Zap size={15}/>{entry.xp.toLocaleString("vi-VN")} XP</strong></article>; })}</div>{visibleEntries.length > pageSize && <button className="leaderboard-secondary leaderboard-load-more" onClick={() => setPageSize((size) => size + 20)}>Xem thêm vị trí</button>}</>}
      </>}
    </section>
  </main>;
}
