"use client";

import React, { useEffect, useState } from "react";
import { readQuizProgress } from "@/lib/quiz-progress";
import {
  Trophy,
  Flame,
  Zap,
  Users,
  User,
  Crown,
  Medal,
  ChevronUp,
  ChevronDown,
  Minus,
  Sparkles,
  Award,
  Target,
  BookOpen,
  Calendar,
  Lock,
  CheckCircle2,
  X,
  Share2,
  TrendingUp,
  Info,
  Smartphone,
  Monitor,
} from "lucide-react";

// ==========================================
// 1. DATA TYPES & INTERFACES
// ==========================================

export interface LeaderboardEntry {
  id: string;
  rank: number;
  previousRank: number;
  name: string;
  avatar: string;
  level: number;
  tag?: string;
  wordsLearned: number;
  xp: number;
  isCurrentUser?: boolean;
  streakDays?: number;
  clanName?: string;
  memberCount?: number;
}

export interface DailyProgress {
  day: string;
  fullDay: string;
  date: string;
  words: number;
  target: number;
  xpEarned: number;
  accuracy: number;
  isToday?: boolean;
}

export interface BadgeItem {
  id: string;
  name: string;
  category: "streak" | "vocabulary" | "speed" | "social";
  rarity: "common" | "rare" | "epic" | "legendary";
  icon: string;
  description: string;
  unlocked: boolean;
  unlockedDate?: string;
  progress: number;
  maxProgress: number;
  xpReward: number;
  lore: string;
}

// ==========================================
// 2. MOCK DATA
// ==========================================

const PERSONAL_RANKINGS: LeaderboardEntry[] = [
  {
    id: "user-1",
    rank: 1,
    previousRank: 1,
    name: "Alex Đặng",
    avatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
    level: 28,
    wordsLearned: 385,
    xp: 4920,
    streakDays: 45,
    clanName: "BK Lexicon",
  },
  {
    id: "user-2",
    rank: 2,
    previousRank: 3,
    name: "Minh Anh Lê",
    avatar: "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80",
    level: 24,
    wordsLearned: 342,
    xp: 4350,
    streakDays: 32,
    clanName: "IELTS 8.0 Club",
  },
  {
    id: "user-3",
    rank: 3,
    previousRank: 2,
    name: "Quang Vũ",
    avatar: "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80",
    level: 21,
    wordsLearned: 310,
    xp: 3980,
    streakDays: 28,
    clanName: "BK Lexicon",
  },
  {
    id: "user-4",
    rank: 4,
    previousRank: 6,
    name: "Thu Thảo",
    avatar: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80",
    level: 19,
    wordsLearned: 280,
    xp: 3510,
    streakDays: 19,
    clanName: "NCC Master",
  },
  {
    id: "user-5",
    rank: 5,
    previousRank: 4,
    name: "Hữu Trí",
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
    level: 18,
    wordsLearned: 255,
    xp: 3240,
    streakDays: 14,
    clanName: "LinguaMezon",
  },
  {
    id: "user-6",
    rank: 6,
    previousRank: 5,
    name: "Bảo Ngọc",
    avatar: "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=150&auto=format&fit=crop&q=80",
    level: 17,
    wordsLearned: 240,
    xp: 3080,
    streakDays: 12,
  },
  {
    id: "user-7",
    rank: 7,
    previousRank: 8,
    name: "Hoàng Nam",
    avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80",
    level: 16,
    wordsLearned: 218,
    xp: 2850,
    streakDays: 11,
    clanName: "BK Lexicon",
  },
  {
    id: "user-current",
    rank: 8,
    previousRank: 10,
    name: "Bạn (Công Ngô)",
    avatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150&auto=format&fit=crop&q=80",
    level: 15,
    wordsLearned: 205,
    xp: 2680,
    streakDays: 14,
    clanName: "LinguaMezon",
    isCurrentUser: true,
  },
  {
    id: "user-9",
    rank: 9,
    previousRank: 7,
    name: "Phương Linh",
    avatar: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&auto=format&fit=crop&q=80",
    level: 14,
    wordsLearned: 192,
    xp: 2490,
    streakDays: 9,
  },
  {
    id: "user-10",
    rank: 10,
    previousRank: 11,
    name: "Tuấn Kiệt",
    avatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&auto=format&fit=crop&q=80",
    level: 13,
    wordsLearned: 180,
    xp: 2310,
    streakDays: 8,
    clanName: "IELTS 8.0 Club",
  },
];

const CLAN_RANKINGS: LeaderboardEntry[] = [
  {
    id: "clan-1",
    rank: 1,
    previousRank: 1,
    name: "BK Lexicon Legion",
    avatar: "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=150&auto=format&fit=crop&q=80",
    level: 35,
    tag: "BKLX",
    wordsLearned: 4820,
    xp: 62400,
    memberCount: 30,
  },
  {
    id: "clan-2",
    rank: 2,
    previousRank: 2,
    name: "LinguaMezon Squad",
    avatar: "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150&auto=format&fit=crop&q=80",
    level: 31,
    tag: "MEZON",
    wordsLearned: 4210,
    xp: 54900,
    memberCount: 28,
    isCurrentUser: true,
  },
  {
    id: "clan-3",
    rank: 3,
    previousRank: 4,
    name: "IELTS 8.0 Conquerors",
    avatar: "https://images.unsplash.com/photo-1614850523459-c2f4c699c52e?w=150&auto=format&fit=crop&q=80",
    level: 29,
    tag: "IELTS",
    wordsLearned: 3890,
    xp: 50100,
    memberCount: 25,
  },
  {
    id: "clan-4",
    rank: 4,
    previousRank: 3,
    name: "NCC Polyglots",
    avatar: "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=150&auto=format&fit=crop&q=80",
    level: 27,
    tag: "NCC",
    wordsLearned: 3410,
    xp: 44200,
    memberCount: 22,
  },
  {
    id: "clan-5",
    rank: 5,
    previousRank: 5,
    name: "Flashcard Warriors",
    avatar: "https://images.unsplash.com/photo-1563089145-599997674d42?w=150&auto=format&fit=crop&q=80",
    level: 24,
    tag: "WAR",
    wordsLearned: 2980,
    xp: 38600,
    memberCount: 20,
  },
];

const WEEKLY_PROGRESS_DATA: DailyProgress[] = [
  { day: "T2", fullDay: "Thứ Hai", date: "09/10", words: 28, target: 25, xpEarned: 360, accuracy: 92 },
  { day: "T3", fullDay: "Thứ Ba", date: "10/10", words: 35, target: 25, xpEarned: 450, accuracy: 96 },
  { day: "T4", fullDay: "Thứ Tư", date: "11/10", words: 18, target: 25, xpEarned: 240, accuracy: 88 },
  { day: "T5", fullDay: "Thứ Năm", date: "12/10", words: 42, target: 25, xpEarned: 560, accuracy: 98 },
  { day: "T6", fullDay: "Thứ Sáu", date: "13/10", words: 30, target: 25, xpEarned: 400, accuracy: 90 },
  { day: "T7", fullDay: "Thứ Bảy", date: "14/10", words: 48, target: 25, xpEarned: 620, accuracy: 95 },
  { day: "CN", fullDay: "Chủ Nhật", date: "15/10", words: 32, target: 25, xpEarned: 430, accuracy: 94, isToday: true },
];

const BADGES_DATA: BadgeItem[] = [
  {
    id: "b-1",
    name: "Kỷ Lục Gia Streak",
    category: "streak",
    rarity: "legendary",
    icon: "🔥",
    description: "Duy trì chuỗi học liên tục trong 14 ngày không đứt quãng.",
    unlocked: true,
    unlockedDate: "Hôm qua",
    progress: 14,
    maxProgress: 14,
    xpReward: 500,
    lore: "Ngọn lửa kiên trì rực cháy thắp sáng con đường làm chủ ngoại ngữ.",
  },
  {
    id: "b-2",
    name: "Kho Báu Từ Vựng",
    category: "vocabulary",
    rarity: "epic",
    icon: "📚",
    description: "Thành thạo vượt mốc 200 từ vựng A1-A2 theo thuật toán SRS SM-2.",
    unlocked: true,
    unlockedDate: "13/10/2026",
    progress: 205,
    maxProgress: 200,
    xpReward: 350,
    lore: "Một trí tuệ sắc bén lưu giữ hàng trăm từ ngữ sống động.",
  },
  {
    id: "b-3",
    name: "Bất Khả Chiến Bại",
    category: "speed",
    rarity: "rare",
    icon: "⚡",
    description: "Đạt tỷ lệ ghi nhớ chính xác 100% trong 5 bài kiểm tra liên tiếp.",
    unlocked: true,
    unlockedDate: "11/10/2026",
    progress: 5,
    maxProgress: 5,
    xpReward: 250,
    lore: "Tốc độ và sự chuẩn xác kết hợp tạo nên phản xạ phi thường.",
  },
  {
    id: "b-4",
    name: "Chiến Hữu Mezon",
    category: "social",
    rarity: "common",
    icon: "🛡️",
    description: "Gia nhập Clan và cùng đồng đội đạt 5,000 XP tuần.",
    unlocked: true,
    unlockedDate: "08/10/2026",
    progress: 5000,
    maxProgress: 5000,
    xpReward: 150,
    lore: "Sức mạnh tập thể nâng bước từng cá nhân vươn lên đỉnh vinh quang.",
  },
  {
    id: "b-5",
    name: "Đại Sứ Học Thuật",
    category: "vocabulary",
    rarity: "epic",
    icon: "🎯",
    description: "Hoàn thành toàn bộ gói từ vựng 500 từ cốt lõi chuẩn Bách Khoa.",
    unlocked: false,
    progress: 205,
    maxProgress: 500,
    xpReward: 750,
    lore: "Mục tiêu tối thượng của bất kỳ học giả ngôn ngữ nào trên Mezon.",
  },
  {
    id: "b-6",
    name: "Đấu Vương Thời Gian Thực",
    category: "speed",
    rarity: "legendary",
    icon: "👑",
    description: "Chiến thắng 20 trận đối kháng 1vs1 Realtime GameHub không thua.",
    unlocked: false,
    progress: 13,
    maxProgress: 20,
    xpReward: 1000,
    lore: "Huyền thoại đấu trường trí tuệ được tôn kính trong toàn server Mezon.",
  },
];

function BadgeCategoryIcon({ category, className = "" }: { category: BadgeItem["category"]; className?: string }) {
  const Icon = category === "streak" ? Flame : category === "vocabulary" ? BookOpen : category === "speed" ? Zap : Users;
  return <Icon className={className} aria-hidden="true" />;
}

// ==========================================
// 3. MAIN COMPONENT
// ==========================================

export default function LeaderboardPage() {
  // Navigation / Filter States
  const [activeTab, setActiveTab] = useState<"personal" | "clan">("personal");
  const [timeRange, setTimeRange] = useState<"weekly" | "monthly" | "alltime">("weekly");
  const [selectedBadge, setSelectedBadge] = useState<BadgeItem | null>(null);
  const [shareFeedback, setShareFeedback] = useState<string | null>(null);
  const [activeBarDay, setActiveBarDay] = useState<DailyProgress | null>(WEEKLY_PROGRESS_DATA[6]);
  const [badgeFilter, setBadgeFilter] = useState<"all" | "unlocked" | "locked">("all");
  const [isMobileFrameView, setIsMobileFrameView] = useState<boolean>(false);
  const [quizXp, setQuizXp] = useState(0);

  useEffect(() => {
    const syncQuizProgress = () => {
      const since = timeRange === "weekly" ? 7 : timeRange === "monthly" ? 30 : Infinity;
      const cutoff = Date.now() - since * 24 * 60 * 60 * 1000;
      const xp = readQuizProgress()
        .filter((record) => new Date(record.completedAt).getTime() >= cutoff)
        .reduce((total, record) => total + record.xp, 0);
      setQuizXp(xp);
    };
    syncQuizProgress();
    window.addEventListener("storage", syncQuizProgress);
    window.addEventListener("lingual:quiz-progress", syncQuizProgress);
    return () => {
      window.removeEventListener("storage", syncQuizProgress);
      window.removeEventListener("lingual:quiz-progress", syncQuizProgress);
    };
  }, [timeRange]);

  const handleShareBadge = async () => {
    if (!selectedBadge) return;
    try {
      await navigator.clipboard.writeText(`${selectedBadge.name} — ${selectedBadge.description}`);
      setShareFeedback("Đã sao chép nội dung chia sẻ.");
    } catch {
      setShareFeedback("Không thể truy cập bộ nhớ tạm trên thiết bị này.");
    }
  };

  useEffect(() => {
    if (!selectedBadge) return;
    const handleDialogKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelectedBadge(null);
    };
    window.addEventListener("keydown", handleDialogKeyDown);
    return () => window.removeEventListener("keydown", handleDialogKeyDown);
  }, [selectedBadge]);

  // Active ranking data
  const currentList = activeTab === "personal"
    ? PERSONAL_RANKINGS.map((entry) => entry.isCurrentUser ? { ...entry, xp: entry.xp + quizXp } : entry)
    : CLAN_RANKINGS;
  const topThree = currentList.slice(0, 3);
  const restList = currentList.slice(3);
  const currentUser = currentList.find((item) => item.isCurrentUser);
  const personalUserXp = (PERSONAL_RANKINGS.find((item) => item.isCurrentUser)?.xp ?? 0) + quizXp;

  // Statistics calculation
  const totalWeeklyWords = WEEKLY_PROGRESS_DATA.reduce((acc, curr) => acc + curr.words, 0);
  const weeklyTarget = WEEKLY_PROGRESS_DATA.reduce((acc, curr) => acc + curr.target, 0);
  const targetPercent = Math.min(100, Math.round((totalWeeklyWords / weeklyTarget) * 100));
  // Filtered badges
  const filteredBadges = BADGES_DATA.filter((badge) => {
    if (badgeFilter === "unlocked") return badge.unlocked;
    if (badgeFilter === "locked") return !badge.unlocked;
    return true;
  });

  return (
    <div className="home-page min-h-screen bg-[#070b14] text-slate-100 antialiased selection:bg-indigo-500 selection:text-white flex flex-col items-center justify-start p-0 md:py-6 md:px-4 relative overflow-x-hidden">
      {/* Background ambient lighting effects */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
        <div className="absolute -top-40 left-1/2 -translate-x-1/2 w-[720px] h-[360px] bg-gradient-to-b from-indigo-600/20 via-purple-600/10 to-transparent blur-[120px] rounded-full" />
        <div className="absolute top-[35%] -left-32 w-80 h-80 bg-amber-500/10 blur-[100px] rounded-full" />
        <div className="absolute top-[60%] -right-32 w-96 h-96 bg-cyan-500/10 blur-[120px] rounded-full" />
      </div>

      {/* Main Container - Can toggle Webview preview frame or full responsive fluid layout */}
      <div
        className={`w-full transition-all duration-300 z-10 flex flex-col ${
          isMobileFrameView
            ? "max-w-[420px] my-4 rounded-[40px] border-[8px] border-slate-800 bg-[#090d1a] shadow-2xl shadow-indigo-950/60 overflow-hidden min-h-[880px]"
            : "max-w-7xl mx-auto"
        }`}
      >
        {/* ========================================================= */}
        {/* TOP BAR / MEZON WEBVIEW CONTROLS & HEADER */}
        {/* ========================================================= */}
        <header className="sticky top-0 z-30 backdrop-blur-xl bg-[#090e1cd0] border-b border-white/5 px-4 py-3 md:px-6 md:py-4 transition-all">
          <div className="flex items-center justify-between gap-3">
            {/* Left: App Identity & Navigation */}
            <div className="flex items-center gap-3">
              <div className="relative flex items-center justify-center w-10 h-10 rounded-2xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-cyan-400 p-[2px] shadow-lg shadow-indigo-500/25">
                <div className="w-full h-full bg-[#0b0f1d] rounded-[14px] flex items-center justify-center">
                  <Trophy className="w-5 h-5 text-amber-400 animate-pulse" />
                </div>
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold uppercase tracking-wider text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded-full border border-indigo-500/20">
                    LinguaMezon
                  </span>
                  <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
                    Mùa giải Sprint 1
                  </span>
                </div>
                <h1 className="text-lg md:text-2xl font-black tracking-tight text-white flex items-center gap-2">
                  Bảng Xếp Hạng & Thống Kê
                </h1>
              </div>
            </div>

            {/* Right: Quick User Summary Pills + Mobile Simulator Toggle */}
            <div className="flex items-center gap-2">
              {/* Toggle Webview preview simulator for demoing */}
              <button
                onClick={() => setIsMobileFrameView(!isMobileFrameView)}
                title="Chuyển chế độ xem mô phỏng Mezon Mobile Webview"
                className="hidden md:flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-300 border border-slate-700/80 transition-colors shadow-sm"
              >
                {isMobileFrameView ? (
                  <>
                    <Monitor className="w-3.5 h-3.5 text-cyan-400" />
                    <span>Xem Desktop</span>
                  </>
                ) : (
                  <>
                    <Smartphone className="w-3.5 h-3.5 text-indigo-400" />
                    <span>Mô phỏng Mezon Webview</span>
                  </>
                )}
              </button>

              {/* User quick pill */}
              <div className="flex items-center gap-2 bg-gradient-to-r from-slate-900/90 to-indigo-950/60 border border-indigo-500/20 px-3 py-1.5 rounded-2xl shadow-inner">
                <div className="flex items-center gap-1 text-amber-400 font-black text-xs md:text-sm">
                  <Flame className="w-4 h-4 fill-amber-400" />
                  <span>14d</span>
                </div>
                <div className="h-3 w-[1px] bg-slate-700" />
                <div className="flex items-center gap-1 text-cyan-400 font-bold text-xs md:text-sm">
                  <Zap className="w-4 h-4 fill-cyan-400" />
                  <span>{personalUserXp.toLocaleString()} XP</span>
                </div>
              </div>
            </div>
          </div>

          {/* Time range switcher (Weekly / Monthly / All-time) */}
          <div className="flex items-center justify-between mt-3 pt-3 border-t border-white/5">
            <div className="text-xs text-slate-400 flex items-center gap-1">
              <Calendar className="w-3.5 h-3.5 text-slate-400" />
              <span>Cập nhật lúc 03:00 hôm nay</span>
            </div>

            <div className="flex p-0.5 bg-slate-900/90 rounded-xl border border-white/10 text-xs font-semibold">
              <button
                onClick={() => setTimeRange("weekly")}
                className={`px-3 py-1 rounded-lg transition-all ${
                  timeRange === "weekly"
                    ? "bg-indigo-600 text-white shadow-md shadow-indigo-600/40"
                    : "text-slate-400 hover:text-white"
                }`}
              >
                Tuần này
              </button>
              <button
                onClick={() => setTimeRange("monthly")}
                className={`px-3 py-1 rounded-lg transition-all ${
                  timeRange === "monthly"
                    ? "bg-indigo-600 text-white shadow-md shadow-indigo-600/40"
                    : "text-slate-400 hover:text-white"
                }`}
              >
                Tháng này
              </button>
              <button
                onClick={() => setTimeRange("alltime")}
                className={`px-3 py-1 rounded-lg transition-all ${
                  timeRange === "alltime"
                    ? "bg-indigo-600 text-white shadow-md shadow-indigo-600/40"
                    : "text-slate-400 hover:text-white"
                }`}
              >
                Toàn thời gian
              </button>
            </div>
          </div>
        </header>

        {/* ========================================================= */}
        {/* MAIN BODY: 2-COLUMN ON DESKTOP, STACKED ON MOBILE */}
        {/* ========================================================= */}
        <main className="p-4 md:p-6 pb-28 md:pb-8 grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          {/* ===================================================== */}
          {/* LEFT COLUMN: LEADERBOARD SECTION (lg:col-span-7) */}
          {/* ===================================================== */}
          <section className="lg:col-span-7 flex flex-col gap-5">
            {/* Mode Switcher: Personal (Cá nhân) vs Clan (Bang hội) */}
            <div className="bg-slate-900/80 backdrop-blur-md p-1.5 rounded-2xl border border-white/10 flex items-center shadow-lg shadow-black/30">
              <button
                onClick={() => setActiveTab("personal")}
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-bold transition-all duration-200 ${
                  activeTab === "personal"
                    ? "bg-gradient-to-r from-indigo-600 to-indigo-500 text-white shadow-lg shadow-indigo-600/30 scale-[1.01]"
                    : "text-slate-400 hover:text-slate-200 hover:bg-white/5"
                }`}
              >
                <User className="w-4 h-4" />
                <span>BXH Cá Nhân</span>
                <span className="text-[10px] bg-indigo-950/80 text-indigo-300 font-extrabold px-1.5 py-0.5 rounded-md border border-indigo-400/20">
                  Global
                </span>
              </button>

              <button
                onClick={() => setActiveTab("clan")}
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-bold transition-all duration-200 ${
                  activeTab === "clan"
                    ? "bg-gradient-to-r from-indigo-600 to-cyan-600 text-white shadow-lg shadow-cyan-600/30 scale-[1.01]"
                    : "text-slate-400 hover:text-slate-200 hover:bg-white/5"
                }`}
              >
                <Users className="w-4 h-4" />
                <span>BXH Bang Hội (Clan)</span>
                <span className="text-[10px] bg-cyan-950/80 text-cyan-300 font-extrabold px-1.5 py-0.5 rounded-md border border-cyan-400/20">
                  Team 05
                </span>
              </button>
            </div>

            {/* Top 3 Podium Card */}
            <div className="relative rounded-3xl bg-gradient-to-b from-indigo-950/40 via-slate-900/80 to-slate-950/90 border border-white/10 p-4 md:p-6 shadow-2xl backdrop-blur-xl overflow-hidden">
              {/* Podium header badge */}
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2">
                  <Sparkles className="w-4 h-4 text-amber-400" />
                  <h2 className="text-sm font-extrabold uppercase tracking-wider text-slate-200">
                    Bục Vinh Danh Top 3
                  </h2>
                </div>
                <span className="text-xs text-amber-400/90 bg-amber-400/10 px-2.5 py-1 rounded-full border border-amber-400/20 font-bold flex items-center gap-1">
                  <Crown className="w-3.5 h-3.5 fill-amber-400" />
                  Phần thưởng x2 XP
                </span>
              </div>

              {/* 3D Podium Layout: Order [Rank 2, Rank 1, Rank 3] */}
              <div className="grid grid-cols-3 gap-2 md:gap-4 items-end pt-6 pb-2">
                {/* 🥈 TOP 2 (SILVER) */}
                {topThree[1] && (
                  <div className="flex flex-col items-center text-center group">
                    <div className="relative mb-2">
                      <div className="w-14 h-14 md:w-20 md:h-20 rounded-2xl p-[2px] bg-gradient-to-b from-slate-200 via-slate-400 to-slate-600 shadow-lg shadow-slate-400/20 group-hover:scale-105 transition-transform duration-300">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={topThree[1].avatar}
                          alt={topThree[1].name}
                          className="w-full h-full object-cover rounded-[14px]"
                        />
                      </div>
                      <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 w-6 h-6 rounded-full bg-gradient-to-br from-slate-200 to-slate-500 border-2 border-[#090d1a] flex items-center justify-center font-black text-xs text-slate-900 shadow">
                        2
                      </div>
                    </div>
                    <span className="font-bold text-xs md:text-sm text-slate-200 truncate max-w-full px-1">
                      {topThree[1].name}
                    </span>
                    <span className="text-[11px] text-slate-400 font-medium">
                      {topThree[1].wordsLearned} từ •{" "}
                      <span className="text-indigo-400 font-bold">{topThree[1].xp} XP</span>
                    </span>

                    {/* Podium Pillar Rank 2 */}
                    <div className="w-full mt-3 h-20 md:h-28 rounded-t-2xl bg-gradient-to-b from-slate-700/60 to-slate-900/80 border-t-2 border-slate-300/40 flex flex-col items-center justify-center p-2 shadow-inner">
                      <Medal className="w-6 h-6 text-slate-300 mb-1" />
                      <span className="text-xs font-black text-slate-300 tracking-wider">HẠNG BẠC</span>
                    </div>
                  </div>
                )}

                {/* 🥇 TOP 1 (GOLD - CENTER & TALLEST) */}
                {topThree[0] && (
                  <div className="flex flex-col items-center text-center -mt-6 group">
                    {/* Crown Icon */}
                    <div className="relative mb-2">
                      <div className="absolute -top-6 left-1/2 -translate-x-1/2 animate-bounce">
                        <Crown className="w-7 h-7 text-amber-400 fill-amber-400 drop-shadow-[0_0_12px_rgba(251,191,36,0.8)]" />
                      </div>
                      <div className="w-18 h-18 md:w-24 md:h-24 rounded-2xl p-[3px] bg-gradient-to-b from-amber-200 via-amber-400 to-amber-600 shadow-xl shadow-amber-500/30 group-hover:scale-105 transition-transform duration-300">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={topThree[0].avatar}
                          alt={topThree[0].name}
                          className="w-full h-full object-cover rounded-[13px]"
                        />
                      </div>
                      <div className="absolute -bottom-2.5 left-1/2 -translate-x-1/2 w-7 h-7 rounded-full bg-gradient-to-br from-amber-300 via-yellow-400 to-amber-600 border-2 border-[#090d1a] flex items-center justify-center font-black text-sm text-amber-950 shadow-md">
                        1
                      </div>
                    </div>

                    <span className="font-extrabold text-sm md:text-base text-amber-200 truncate max-w-full px-1 flex items-center gap-1">
                      {topThree[0].name}
                    </span>
                    <span className="text-xs text-amber-300/90 font-semibold">
                      {topThree[0].wordsLearned} từ •{" "}
                      <span className="text-amber-400 font-bold">{topThree[0].xp} XP</span>
                    </span>

                    {/* Podium Pillar Rank 1 */}
                    <div className="w-full mt-3 h-28 md:h-36 rounded-t-2xl bg-gradient-to-b from-amber-500/30 via-amber-600/20 to-slate-900/90 border-t-4 border-amber-400 flex flex-col items-center justify-center p-2 shadow-inner relative overflow-hidden">
                      <div className="absolute inset-0 bg-gradient-to-t from-transparent via-amber-400/5 to-amber-400/20 pointer-events-none" />
                      <Trophy className="w-8 h-8 text-amber-400 mb-1 fill-amber-400/50" />
                      <span className="text-xs md:text-sm font-black text-amber-300 tracking-wider">QUÁN QUÂN</span>
                      <span className="text-[10px] text-amber-400/80 font-bold mt-0.5">Top 1 Server</span>
                    </div>
                  </div>
                )}

                {/* 🥉 TOP 3 (BRONZE) */}
                {topThree[2] && (
                  <div className="flex flex-col items-center text-center group">
                    <div className="relative mb-2">
                      <div className="w-14 h-14 md:w-20 md:h-20 rounded-2xl p-[2px] bg-gradient-to-b from-amber-600 via-amber-700 to-amber-900 shadow-lg shadow-amber-800/20 group-hover:scale-105 transition-transform duration-300">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={topThree[2].avatar}
                          alt={topThree[2].name}
                          className="w-full h-full object-cover rounded-[14px]"
                        />
                      </div>
                      <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 w-6 h-6 rounded-full bg-gradient-to-br from-amber-500 to-amber-800 border-2 border-[#090d1a] flex items-center justify-center font-black text-xs text-amber-100 shadow">
                        3
                      </div>
                    </div>
                    <span className="font-bold text-xs md:text-sm text-slate-200 truncate max-w-full px-1">
                      {topThree[2].name}
                    </span>
                    <span className="text-[11px] text-slate-400 font-medium">
                      {topThree[2].wordsLearned} từ •{" "}
                      <span className="text-indigo-400 font-bold">{topThree[2].xp} XP</span>
                    </span>

                    {/* Podium Pillar Rank 3 */}
                    <div className="w-full mt-3 h-16 md:h-24 rounded-t-2xl bg-gradient-to-b from-amber-900/40 to-slate-900/80 border-t-2 border-amber-600/40 flex flex-col items-center justify-center p-2 shadow-inner">
                      <Medal className="w-5 h-5 text-amber-500 mb-1" />
                      <span className="text-[11px] font-black text-amber-500 tracking-wider">HẠNG ĐỒNG</span>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Ranks 4 to 10 List */}
            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between px-2 text-xs font-semibold text-slate-400">
                <span>Thứ hạng & Chiến binh</span>
                <span>Tiến độ từ vựng & XP</span>
              </div>

              <div className="flex flex-col gap-2.5">
                {restList.map((entry) => {
                  const rankDiff = entry.previousRank - entry.rank;
                  const isUser = entry.isCurrentUser;

                  return (
                    <div
                      key={entry.id}
                      className={`group relative flex items-center justify-between p-3 md:p-3.5 rounded-2xl border transition-all duration-200 ${
                        isUser
                          ? "bg-gradient-to-r from-indigo-950/80 via-indigo-900/40 to-slate-900/90 border-indigo-500/50 shadow-md shadow-indigo-600/20 ring-1 ring-indigo-500/30"
                          : "bg-slate-900/60 hover:bg-slate-800/80 border-white/5 hover:border-white/10"
                      }`}
                    >
                      {/* Left: Rank & Avatar & Info */}
                      <div className="flex items-center gap-3 min-w-0">
                        {/* Rank & Movement Indicator */}
                        <div className="flex flex-col items-center justify-center w-7 shrink-0">
                          <span
                            className={`font-black text-sm md:text-base ${
                              isUser ? "text-indigo-300" : "text-slate-300"
                            }`}
                          >
                            #{entry.rank}
                          </span>
                          <div className="flex items-center text-[10px] font-bold">
                            {rankDiff > 0 ? (
                              <span className="text-emerald-400 flex items-center">
                                <ChevronUp className="w-3 h-3" />
                                {rankDiff}
                              </span>
                            ) : rankDiff < 0 ? (
                              <span className="text-rose-400 flex items-center">
                                <ChevronDown className="w-3 h-3" />
                                {Math.abs(rankDiff)}
                              </span>
                            ) : (
                              <span className="text-slate-500">
                                <Minus className="w-2.5 h-2.5" />
                              </span>
                            )}
                          </div>
                        </div>

                        {/* Avatar with optional Level Ring */}
                        <div className="relative shrink-0">
                          {/* eslint-disable-next-line @next/next/no-img-element */}
                          <img
                            src={entry.avatar}
                            alt={entry.name}
                            className={`w-10 h-10 md:w-11 md:h-11 rounded-xl object-cover border ${
                              isUser ? "border-indigo-400" : "border-white/10"
                            }`}
                          />
                          <span className="absolute -bottom-1 -right-1 bg-slate-900 text-[10px] font-bold px-1 rounded-md text-amber-400 border border-white/10">
                            {entry.level}
                          </span>
                        </div>

                        {/* Name & Clan details */}
                        <div className="min-w-0 flex flex-col">
                          <div className="flex items-center gap-2">
                            <span
                              className={`font-bold text-sm truncate ${
                                isUser ? "text-white flex items-center gap-1.5" : "text-slate-200"
                              }`}
                            >
                              {entry.name}
                              {isUser && (
                                <span className="bg-indigo-600 text-[10px] font-extrabold px-1.5 py-0.2 rounded text-white uppercase tracking-wider">
                                  Bạn
                                </span>
                              )}
                            </span>
                          </div>

                          <div className="flex items-center gap-2 text-xs text-slate-400">
                            {entry.clanName && (
                              <span className="text-slate-400 flex items-center gap-1">
                                <Users className="w-3 h-3 text-slate-500" />
                                <span className="truncate max-w-[100px] md:max-w-[140px]">{entry.clanName}</span>
                              </span>
                            )}
                            {entry.streakDays && (
                              <span className="flex items-center gap-0.5 text-amber-400/90 font-medium">
                                <Flame className="w-3 h-3 fill-amber-400" />
                                {entry.streakDays}d
                              </span>
                            )}
                            {entry.memberCount && (
                              <span className="text-cyan-400/90 font-medium">
                                {entry.memberCount} thành viên
                              </span>
                            )}
                          </div>
                        </div>
                      </div>

                      {/* Right: Score Metrics */}
                      <div className="flex flex-col items-end shrink-0 pl-2">
                        <span className="font-extrabold text-sm md:text-base text-white flex items-center gap-1">
                          {entry.wordsLearned.toLocaleString()} <span className="text-xs font-medium text-slate-400">từ</span>
                        </span>
                        <span className="text-xs font-semibold text-indigo-400 flex items-center gap-0.5">
                          <Zap className="w-3 h-3 fill-indigo-400" />
                          {entry.xp.toLocaleString()} XP
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </section>

          {/* ===================================================== */}
          {/* RIGHT COLUMN: PROGRESS STATS & HONOR BADGES (lg:col-span-5) */}
          {/* ===================================================== */}
          <div className="lg:col-span-5 flex flex-col gap-6">
            {/* 1. PROGRESS STATISTICS CARD WITH BAR CHART */}
            <section className="rounded-3xl bg-slate-900/80 border border-white/10 p-5 shadow-2xl backdrop-blur-xl relative overflow-hidden">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2">
                  <div className="p-2 rounded-xl bg-indigo-500/10 border border-indigo-500/20 text-indigo-400">
                    <TrendingUp className="w-4 h-4" />
                  </div>
                  <div>
                    <h2 className="text-base font-black text-white">Tiến Độ Học Tập Tuần</h2>
                    <p className="text-xs text-slate-400">Thứ 2 đến Chủ Nhật (SRS SM-2)</p>
                  </div>
                </div>

                <div className="text-right">
                  <span className="text-xs font-bold text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                    {targetPercent}% Mục tiêu
                  </span>
                </div>
              </div>

              {/* Metric Highlights Pill Cards */}
              <div className="grid grid-cols-3 gap-2.5 mb-5">
                <div className="bg-slate-950/60 p-2.5 rounded-2xl border border-white/5 flex flex-col">
                  <span className="text-[10px] font-semibold text-slate-400 uppercase">Tổng từ</span>
                  <div className="flex items-baseline gap-1 mt-0.5">
                    <span className="text-lg font-black text-white">{totalWeeklyWords}</span>
                    <span className="text-[10px] text-slate-500">/ 175</span>
                  </div>
                  <span className="text-[10px] text-emerald-400 font-medium">Đạt chuẩn tuần</span>
                </div>

                <div className="bg-slate-950/60 p-2.5 rounded-2xl border border-white/5 flex flex-col">
                  <span className="text-[10px] font-semibold text-slate-400 uppercase">Trung bình</span>
                  <div className="flex items-baseline gap-1 mt-0.5">
                    <span className="text-lg font-black text-indigo-400">
                      {Math.round(totalWeeklyWords / 7)}
                    </span>
                    <span className="text-[10px] text-slate-500">từ/ngày</span>
                  </div>
                  <span className="text-[10px] text-indigo-300 font-medium">+15% so với T.trước</span>
                </div>

                <div className="bg-slate-950/60 p-2.5 rounded-2xl border border-white/5 flex flex-col">
                  <span className="text-[10px] font-semibold text-slate-400 uppercase">Độ chính xác</span>
                  <div className="flex items-baseline gap-1 mt-0.5">
                    <span className="text-lg font-black text-amber-400">94.2%</span>
                  </div>
                  <span className="text-[10px] text-amber-300 font-medium">Cấp độ A2+</span>
                </div>
              </div>

              {/* CSS Interactive Bar Chart (Mon - Sun) */}
              <div className="pt-2">
                <div className="flex items-center justify-between text-xs text-slate-400 mb-2">
                  <span className="flex items-center gap-1.5 font-medium">
                    <BookOpen className="w-3.5 h-3.5 text-indigo-400" />
                    Số từ vựng đã học theo ngày
                  </span>
                  <span className="text-[11px] text-slate-500">
                    Mục tiêu ngày: <strong className="text-slate-300">25 từ</strong>
                  </span>
                </div>

                {/* Target threshold reference line indicator */}
                <div className="relative pt-6 pb-2">
                  <div className="absolute top-12 left-0 right-0 border-b border-dashed border-indigo-400/20 z-0">
                    <span className="absolute -top-3 right-0 text-[9px] font-bold text-indigo-400/60 bg-[#0c1222] px-1">
                      Mục tiêu (25)
                    </span>
                  </div>

                  {/* Bars container */}
                  <div className="grid grid-cols-7 gap-2 md:gap-3 items-end h-44 relative z-10 px-1">
                    {WEEKLY_PROGRESS_DATA.map((item) => {
                      const heightPercent = Math.min(100, Math.round((item.words / 55) * 100));
                      const isSelected = activeBarDay?.day === item.day;
                      const reachedTarget = item.words >= item.target;

                      return (
                        <div
                          key={item.day}
                          onClick={() => setActiveBarDay(item)}
                          className="flex flex-col items-center h-full justify-end cursor-pointer group"
                        >
                          {/* Value on hover or selected */}
                          <span
                            className={`text-[10px] font-bold mb-1 transition-all ${
                              isSelected
                                ? "text-cyan-300 opacity-100 scale-110"
                                : "text-slate-400 opacity-0 group-hover:opacity-100"
                            }`}
                          >
                            {item.words}
                          </span>

                          {/* Bar Pillar */}
                          <div className="w-full max-w-[28px] h-32 bg-slate-800/70 rounded-xl p-0.5 flex flex-col justify-end overflow-hidden border border-white/5 group-hover:border-indigo-400/40 transition-colors">
                            <div
                              style={{ height: `${heightPercent}%` }}
                              className={`w-full rounded-lg transition-all duration-500 relative ${
                                item.isToday
                                  ? "bg-gradient-to-t from-cyan-500 via-indigo-500 to-amber-300 shadow-lg shadow-cyan-500/30"
                                  : reachedTarget
                                  ? "bg-gradient-to-t from-indigo-600 to-indigo-400 group-hover:from-indigo-500 group-hover:to-cyan-400"
                                  : "bg-gradient-to-t from-slate-600 to-slate-500"
                              } ${isSelected ? "ring-2 ring-white/60" : ""}`}
                            >
                              {/* Highlight cap */}
                              <div className="absolute top-0 inset-x-0 h-1 bg-white/40 rounded-t-lg" />
                            </div>
                          </div>

                          {/* Day Label */}
                          <span
                            className={`text-xs mt-2 font-bold transition-colors ${
                              item.isToday
                                ? "text-amber-400 underline underline-offset-4 decoration-amber-400 decoration-2"
                                : isSelected
                                ? "text-white"
                                : "text-slate-400 group-hover:text-slate-200"
                            }`}
                          >
                            {item.day}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* Selected Day Details Tooltip Banner */}
                {activeBarDay && (
                  <div className="mt-3 p-3 rounded-2xl bg-gradient-to-r from-indigo-950/70 to-slate-900 border border-indigo-500/20 flex items-center justify-between animate-fadeIn">
                    <div className="flex items-center gap-2.5">
                      <div className="w-2.5 h-2.5 rounded-full bg-cyan-400 animate-ping" />
                      <div>
                        <div className="text-xs font-bold text-white flex items-center gap-1.5">
                          <span>{activeBarDay.fullDay} ({activeBarDay.date})</span>
                          {activeBarDay.isToday && (
                            <span className="text-[10px] bg-amber-400/20 text-amber-300 px-1.5 py-0.2 rounded font-extrabold">
                              Hôm nay
                            </span>
                          )}
                        </div>
                        <p className="text-[11px] text-slate-400">
                          Chính xác: <strong className="text-emerald-400">{activeBarDay.accuracy}%</strong> • Thuộc {activeBarDay.words} từ
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <span className="text-xs font-black text-cyan-300">+{activeBarDay.xpEarned} XP</span>
                      <p className="text-[10px] text-slate-400">Đã cộng vào sổ cái</p>
                    </div>
                  </div>
                )}
              </div>
            </section>

            {/* 2. HONOR BADGES SECTION (HUY HIỆU DANH DỰ) */}
            <section className="rounded-3xl bg-slate-900/80 border border-white/10 p-5 shadow-2xl backdrop-blur-xl">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2">
                  <div className="p-2 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400">
                    <Award className="w-4 h-4" />
                  </div>
                  <div>
                    <h2 className="text-base font-black text-white">Huy Hiệu Danh Dự</h2>
                    <p className="text-xs text-slate-400">Thành tích & phần thưởng danh vọng</p>
                  </div>
                </div>

                <span className="text-xs font-bold text-amber-400 bg-amber-400/10 px-2.5 py-1 rounded-full border border-amber-400/20">
                  {BADGES_DATA.filter((b) => b.unlocked).length}/{BADGES_DATA.length} Đã mở
                </span>
              </div>

              {/* Filter Tabs for Badges */}
              <div className="flex gap-1.5 mb-4 text-xs font-semibold">
                <button
                  onClick={() => setBadgeFilter("all")}
                  className={`px-2.5 py-1 rounded-lg transition-colors ${
                    badgeFilter === "all" ? "bg-white/10 text-white" : "text-slate-400 hover:text-slate-200"
                  }`}
                >
                  Tất cả ({BADGES_DATA.length})
                </button>
                <button
                  onClick={() => setBadgeFilter("unlocked")}
                  className={`px-2.5 py-1 rounded-lg transition-colors ${
                    badgeFilter === "unlocked"
                      ? "bg-amber-500/20 text-amber-300 border border-amber-500/30"
                      : "text-slate-400 hover:text-slate-200"
                  }`}
                >
                  Đã đạt ({BADGES_DATA.filter((b) => b.unlocked).length})
                </button>
                <button
                  onClick={() => setBadgeFilter("locked")}
                  className={`px-2.5 py-1 rounded-lg transition-colors ${
                    badgeFilter === "locked"
                      ? "bg-slate-800 text-slate-200 border border-slate-700"
                      : "text-slate-400 hover:text-slate-200"
                  }`}
                >
                  Chưa đạt ({BADGES_DATA.filter((b) => !b.unlocked).length})
                </button>
              </div>

              {/* Badges Grid (Responsive 2 cols on mobile, 3 cols on wider screens) */}
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {filteredBadges.map((badge) => {
                  const progressPct = Math.min(100, Math.round((badge.progress / badge.maxProgress) * 100));

                  return (
                    <button
                      type="button"
                      key={badge.id}
                      onClick={() => { setSelectedBadge(badge); setShareFeedback(null); }}
                      aria-label={`Xem chi tiết danh hiệu ${badge.name}`}
                      aria-haspopup="dialog"
                      className={`group relative p-3 rounded-2xl border transition-all duration-300 cursor-pointer flex flex-col items-center text-center ${
                        badge.unlocked
                          ? badge.rarity === "legendary"
                            ? "bg-gradient-to-b from-amber-500/15 via-slate-900/80 to-slate-950 border-amber-500/40 hover:border-amber-400 hover:shadow-lg hover:shadow-amber-500/20"
                            : badge.rarity === "epic"
                            ? "bg-gradient-to-b from-purple-500/15 via-slate-900/80 to-slate-950 border-purple-500/40 hover:border-purple-400 hover:shadow-lg hover:shadow-purple-500/20"
                            : "bg-gradient-to-b from-indigo-500/15 via-slate-900/80 to-slate-950 border-indigo-500/40 hover:border-indigo-400 hover:shadow-lg hover:shadow-indigo-500/20"
                          : "bg-slate-950/60 border-white/5 opacity-60 hover:opacity-90 grayscale hover:grayscale-0"
                      }`}
                    >
                      {/* Badge Icon with glowing container */}
                      <div className="relative mb-2">
                        <div
                          className={`w-12 h-12 rounded-2xl flex items-center justify-center text-2xl transition-transform duration-300 group-hover:scale-110 ${
                            badge.unlocked
                              ? "bg-slate-800/80 shadow-md border border-white/10"
                              : "bg-slate-900 border border-slate-800"
                          }`}
                        >
                          <BadgeCategoryIcon category={badge.category} className="h-5 w-5" />
                        </div>

                        {/* Lock / Check overlay icon */}
                        {badge.unlocked ? (
                          <div className="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-emerald-500 text-slate-950 flex items-center justify-center shadow">
                            <CheckCircle2 className="w-3 h-3 text-slate-950" />
                          </div>
                        ) : (
                          <div className="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-slate-800 text-slate-400 flex items-center justify-center border border-white/10 shadow">
                            <Lock className="w-2.5 h-2.5" />
                          </div>
                        )}
                      </div>

                      {/* Title & Rarity */}
                      <span className="font-extrabold text-xs text-white truncate max-w-full group-hover:text-indigo-300 transition-colors">
                        {badge.name}
                      </span>

                      <span
                        className={`text-[9px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded-full mt-1 ${
                          badge.rarity === "legendary"
                            ? "text-amber-400 bg-amber-400/10"
                            : badge.rarity === "epic"
                            ? "text-purple-400 bg-purple-400/10"
                            : "text-cyan-400 bg-cyan-400/10"
                        }`}
                      >
                        {badge.rarity}
                      </span>

                      {/* Mini Progress Bar for locked badges */}
                      {!badge.unlocked && (
                        <div className="w-full mt-2">
                          <div className="flex justify-between text-[9px] text-slate-400 font-semibold mb-1">
                            <span>Tiến độ</span>
                            <span>{progressPct}%</span>
                          </div>
                          <div className="w-full h-1 bg-slate-800 rounded-full overflow-hidden">
                            <div
                              style={{ width: `${progressPct}%` }}
                              className="h-full bg-indigo-500 rounded-full"
                            />
                          </div>
                        </div>
                      )}
                    </button>
                  );
                })}
              </div>

              {/* Bottom hint */}
              <div className="mt-4 pt-3 border-t border-white/5 flex items-center justify-between text-xs text-slate-400">
                <span className="flex items-center gap-1">
                  <Info className="w-3.5 h-3.5 text-indigo-400" />
                  Nhấn vào huy hiệu để xem chi tiết & phần thưởng
                </span>
                <span className="text-amber-400 font-bold flex items-center gap-0.5">
                  <Zap className="w-3 h-3 fill-amber-400" />
                  +1,250 XP đã nhận
                </span>
              </div>
            </section>
          </div>
        </main>

        {/* ========================================================= */}
        {/* STICKY BOTTOM PINNED CURRENT USER BAR (OPTIMIZED FOR MEZON WEBVIEW) */}
        {/* ========================================================= */}
        {currentUser && (
          <aside className="fixed md:sticky bottom-0 inset-x-0 z-40 bg-[#090e1ced] backdrop-blur-xl border-t border-indigo-500/30 p-3 md:px-6 shadow-[0_-8px_30px_rgba(0,0,0,0.6)]">
            <div className="max-w-7xl mx-auto flex items-center justify-between gap-3">
              {/* Left: User ranking badge and name */}
              <div className="flex items-center gap-2.5 min-w-0">
                <div className="relative">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={currentUser.avatar}
                    alt={currentUser.name}
                    className="w-10 h-10 rounded-xl object-cover border-2 border-indigo-400"
                  />
                  <div className="absolute -bottom-1 -right-1 bg-indigo-600 text-white font-black text-[10px] px-1 rounded-md shadow">
                    #{currentUser.rank}
                  </div>
                </div>

                <div className="min-w-0">
                  <div className="flex items-center gap-1.5">
                    <span className="font-extrabold text-sm text-white truncate">
                      {currentUser.name}
                    </span>
                    <span className="text-[10px] font-bold text-amber-400 bg-amber-400/10 px-1.5 py-0.2 rounded border border-amber-400/20">
                      Lv.{currentUser.level}
                    </span>
                  </div>
                  <p className="text-[11px] text-indigo-300 font-medium">
                    Cách Top 5: <strong className="text-white">50 từ</strong> • Tăng 2 bậc tuần này!
                  </p>
                </div>
              </div>

              {/* Right: Quick CTA & Score */}
              <div className="flex items-center gap-2.5 shrink-0">
                <div className="text-right hidden sm:block">
                  <span className="text-xs text-slate-400 block">Điểm kinh nghiệm</span>
                  <span className="text-sm font-black text-cyan-400 flex items-center justify-end gap-1">
                    <Zap className="w-3.5 h-3.5 fill-cyan-400" />
                    {currentUser.xp.toLocaleString()} XP
                  </span>
                </div>

                <a
                  href="/learn"
                  className="px-4 py-2 rounded-xl bg-gradient-to-r from-indigo-500 to-cyan-500 hover:from-indigo-600 hover:to-cyan-600 text-white font-extrabold text-xs md:text-sm shadow-lg shadow-indigo-500/25 transition-transform active:scale-95 flex items-center gap-1.5"
                >
                  <Target className="w-3.5 h-3.5" />
                  <span>Học Tiếp Ngay</span>
                </a>
              </div>
            </div>
          </aside>
        )}

        {/* ========================================================= */}
        {/* MODAL: BADGE DETAIL DIALOG */}
        {/* ========================================================= */}
        {selectedBadge && (
          <div className="badge-dialog-backdrop fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/55 backdrop-blur-sm animate-fadeIn" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelectedBadge(null); }}>
            <div role="dialog" aria-modal="true" aria-labelledby="badge-dialog-title" className="badge-dialog relative w-full max-w-sm rounded-3xl bg-[#0c1326] border border-indigo-500/30 p-6 shadow-2xl flex flex-col items-center text-center">
              {/* Close Button */}
              <button
                type="button"
                onClick={() => setSelectedBadge(null)}
                aria-label="Đóng chi tiết danh hiệu"
                className="absolute top-4 right-4 p-1.5 rounded-full bg-slate-800 text-slate-400 hover:text-white transition-colors"
              >
                <X className="w-4 h-4" />
              </button>

              {/* Giant Badge Icon */}
              <div className="relative my-3">
                <div
                  className={`w-20 h-20 rounded-3xl flex items-center justify-center text-4xl shadow-xl ${
                    selectedBadge.unlocked
                      ? "bg-gradient-to-br from-indigo-600 via-purple-600 to-amber-500 shadow-indigo-500/30 border-2 border-white/20"
                      : "bg-slate-800 border-2 border-slate-700 grayscale"
                  }`}
                >
                  <BadgeCategoryIcon category={selectedBadge.category} className="h-8 w-8" />
                </div>
                {selectedBadge.unlocked ? (
                  <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 bg-emerald-500 text-slate-950 font-black text-xs px-2.5 py-0.5 rounded-full shadow flex items-center gap-1">
                    <CheckCircle2 className="w-3 h-3 text-slate-950" />
                    ĐÃ MỞ KHÓA
                  </div>
                ) : (
                  <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 bg-slate-800 text-slate-400 font-bold text-xs px-2.5 py-0.5 rounded-full border border-slate-700 shadow flex items-center gap-1">
                    <Lock className="w-3 h-3" />
                    CHƯA MỞ
                  </div>
                )}
              </div>

              {/* Title & Rarity */}
              <h3 id="badge-dialog-title" className="text-xl font-black text-white mt-3">{selectedBadge.name}</h3>
              <span
                className={`text-[10px] uppercase font-black tracking-widest px-2 py-0.5 rounded-full mt-1 ${
                  selectedBadge.rarity === "legendary"
                    ? "text-amber-400 bg-amber-400/10 border border-amber-400/20"
                    : selectedBadge.rarity === "epic"
                    ? "text-purple-400 bg-purple-400/10 border border-purple-400/20"
                    : "text-cyan-400 bg-cyan-400/10 border border-cyan-400/20"
                }`}
              >
                Phẩm chất {selectedBadge.rarity}
              </span>

              {/* Lore / Story */}
              <p className="text-xs italic text-indigo-300/80 my-3 px-2">
                &ldquo;{selectedBadge.lore}&rdquo;
              </p>

              {/* Requirement & Description */}
              <div className="w-full bg-slate-900/90 rounded-2xl p-3.5 border border-white/5 my-2 text-left">
                <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block mb-1">
                  Yêu Cầu Mở Khóa:
                </span>
                <p className="text-xs text-slate-200 leading-relaxed font-medium">
                  {selectedBadge.description}
                </p>

                {/* Progress bar if locked */}
                {!selectedBadge.unlocked && (
                  <div className="mt-3">
                    <div className="flex justify-between text-xs font-bold text-slate-300 mb-1">
                      <span>Tiến độ thực hiện</span>
                      <span className="text-indigo-400">
                        {selectedBadge.progress} / {selectedBadge.maxProgress}
                      </span>
                    </div>
                    <div className="w-full h-2 bg-slate-800 rounded-full overflow-hidden">
                      <div
                        style={{
                          width: `${Math.min(
                            100,
                            (selectedBadge.progress / selectedBadge.maxProgress) * 100
                          )}%`,
                        }}
                        className="h-full bg-gradient-to-r from-indigo-500 to-cyan-400 rounded-full"
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Rewards */}
              <div className="flex items-center justify-between w-full px-2 py-2 text-xs">
                <span className="text-slate-400">Phần thưởng đạt được:</span>
                <span className="font-black text-amber-400 flex items-center gap-1">
                  <Zap className="w-4 h-4 fill-amber-400" />
                  +{selectedBadge.xpReward} XP
                </span>
              </div>

              {/* Action Buttons */}
              <div className="flex gap-2 w-full mt-4">
                <button
                  type="button"
                  onClick={handleShareBadge}
                  className="flex-1 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs flex items-center justify-center gap-1.5 transition-colors border border-white/5"
                >
                  <Share2 className="w-3.5 h-3.5" />
                  <span>{shareFeedback || "Sao chép nội dung"}</span>
                </button>
                <button
                  type="button"
                  onClick={() => setSelectedBadge(null)}
                  className="flex-1 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-extrabold text-xs transition-colors shadow-lg shadow-indigo-600/30"
                >
                  Đóng
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
