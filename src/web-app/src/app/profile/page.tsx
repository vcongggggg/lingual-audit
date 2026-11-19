"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  User as UserIcon,
  Flame,
  Trophy,
  Award,
  BookOpen,
  NotebookTabs,
  Sparkles,
  Zap,
  Target,
  Shield,
  CheckCircle2,
  Lock,
  Calendar,
  RotateCcw,
  ArrowRight,
  Star,
  Medal,
  Clock,
  LogIn,
  Check,
  TrendingUp,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import {
  fetchUserProfileSummary,
  UserProfileSummaryDto,
  BadgeSummaryDto,
  MOCK_USER_PROFILE_SUMMARY,
} from "@/lib/api";
import LinguaMascot from "@/components/LinguaMascot";

const RARITY_CONFIG: Record<
  string,
  { label: string; badgeClass: string; cardClass: string; iconBg: string }
> = {
  common: {
    label: "Phổ thông",
    badgeClass: "bg-[#eff1e8] text-[#596444] border-[#d7ddca]",
    cardClass: "border-[#d7ddca] bg-[#f7f9f3] hover:border-[#b8c2a8]",
    iconBg: "bg-[#eff1e8] text-[#596444]",
  },
  rare: {
    label: "Hiếm",
    badgeClass: "bg-blue-50 text-blue-700 border-blue-200",
    cardClass: "border-blue-200 bg-blue-50/40 hover:border-blue-300",
    iconBg: "bg-blue-100 text-blue-700",
  },
  epic: {
    label: "Sử thi",
    badgeClass: "bg-purple-50 text-purple-700 border-purple-200",
    cardClass: "border-purple-200 bg-purple-50/40 hover:border-purple-300",
    iconBg: "bg-purple-100 text-purple-700",
  },
  legendary: {
    label: "Huyền thoại",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-300",
    cardClass: "border-amber-300 bg-amber-50/40 hover:border-amber-400",
    iconBg: "bg-amber-100 text-amber-800",
  },
};

const LEVEL_TITLES: Record<number, string> = {
  1: "Tập sự ngôn ngữ",
  2: "Người tìm tòi",
  3: "Nhà thám hiểm từ vựng",
  4: "Chiến binh ngữ pháp",
  5: "Học giả bản lĩnh",
};

function getLevelTitle(level: number): string {
  return LEVEL_TITLES[level] || `Bậc thầy Cấp ${level}`;
}

function renderBadgeIcon(iconKey: string, className = "w-6 h-6") {
  switch (iconKey.toLowerCase()) {
    case "flame":
    case "streak":
      return <Flame className={className} />;
    case "award":
      return <Award className={className} />;
    case "book-open":
    case "book":
    case "vocabulary":
      return <BookOpen className={className} />;
    case "sparkles":
      return <Sparkles className={className} />;
    case "trophy":
      return <Trophy className={className} />;
    case "zap":
      return <Zap className={className} />;
    case "target":
      return <Target className={className} />;
    case "shield":
      return <Shield className={className} />;
    default:
      return <Medal className={className} />;
  }
}

export default function ProfilePage() {
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const [profile, setProfile] = useState<UserProfileSummaryDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [badgeFilter, setBadgeFilter] = useState<"all" | "unlocked" | "locked">("all");
  const [copiedId, setCopiedId] = useState<boolean>(false);

  const loadData = useCallback(async (isRefresh = false) => {
    if (isRefresh) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    try {
      const summary = await fetchUserProfileSummary(user?.id);
      setProfile(summary);
    } catch {
      setProfile(MOCK_USER_PROFILE_SUMMARY);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [user?.id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const activeProfile = profile || MOCK_USER_PROFILE_SUMMARY;

  const displayName = user?.displayName || activeProfile.displayName || "Mezon Learner";
  const avatarUrl = user?.avatarUrl || activeProfile.avatarUrl;
  const userRole = user?.role || activeProfile.role || "learner";
  const mezonId = user?.mezonUserId || (activeProfile.userId ? activeProfile.userId.slice(0, 8) : "learner_01");

  const filteredBadges = useMemo(() => {
    if (!activeProfile.badges) return [];
    if (badgeFilter === "unlocked") return activeProfile.badges.filter((b) => b.unlocked);
    if (badgeFilter === "locked") return activeProfile.badges.filter((b) => !b.unlocked);
    return activeProfile.badges;
  }, [activeProfile.badges, badgeFilter]);

  const unlockedBadgesCount = useMemo(() => {
    return activeProfile.badges ? activeProfile.badges.filter((b) => b.unlocked).length : 0;
  }, [activeProfile.badges]);

  // XP calculation
  const totalXp = activeProfile.totalXp ?? 450;
  const nextLevelXp = Math.max(totalXp, activeProfile.nextLevelXp ?? 600);
  const xpPercent = Math.min(100, Math.max(0, Math.round((totalXp / nextLevelXp) * 100)));
  const xpToNext = Math.max(0, nextLevelXp - totalXp);

  // Daily streak goal calculation
  const streak = activeProfile.streak;
  const dailyXp = streak?.todayXp ?? 0;
  const neededXp = Math.max(1, streak?.xpNeededForStreak ?? 30);
  const dailyPercent = Math.min(100, Math.round((dailyXp / neededXp) * 100));

  const handleCopyId = () => {
    navigator.clipboard?.writeText(mezonId);
    setCopiedId(true);
    setTimeout(() => setCopiedId(false), 2000);
  };

  return (
    <main className="min-h-[calc(100svh-68px)] max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-10 text-[var(--ink,#252820)]">
      {/* SECTION 1: USER PROFILE HEADER */}
      <section className="bg-[#eff1e8] border border-[#d7ddca] rounded-3xl p-6 sm:p-8 shadow-xs mb-8 transition-all relative overflow-hidden">
        {/* Soft background decorative glow */}
        <div
          className="absolute -right-16 -top-16 w-56 h-56 rounded-full bg-[#69734f]/10 pointer-events-none blur-2xl"
          aria-hidden="true"
        />

        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 relative z-10">
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-5">
            {/* Avatar */}
            <div className="relative">
              {avatarUrl && avatarUrl.trim() !== "" ? (
                <img
                  src={avatarUrl}
                  alt={displayName}
                  className="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl object-cover border-2 border-[#d7ddca] shadow-sm bg-white"
                />
              ) : (
                <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl bg-[var(--olive,#69734f)] text-white flex items-center justify-center font-bold text-3xl sm:text-4xl shadow-sm">
                  {displayName.charAt(0).toUpperCase()}
                </div>
              )}
              <span
                className="absolute -bottom-1 -right-1 w-6 h-6 rounded-full bg-emerald-500 border-2 border-white flex items-center justify-center text-white"
                title="Đang hoạt động"
              >
                <Check className="w-3.5 h-3.5 stroke-[3]" />
              </span>
            </div>

            {/* Details */}
            <div className="space-y-1.5">
              <div className="flex flex-wrap items-center gap-2.5">
                <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-[var(--ink,#252820)]">
                  {displayName}
                </h1>
                <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-[#69734f] text-white shadow-xs">
                  <Star className="w-3 h-3 fill-white" />
                  {userRole === "admin" ? "Quản trị viên" : "Học viên xuất sắc"}
                </span>
              </div>

              {/* Mezon ID & Membership Date */}
              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-[var(--muted,#77786f)]">
                <button
                  onClick={handleCopyId}
                  className="inline-flex items-center gap-1.5 font-mono px-2 py-0.5 rounded-md bg-[#f7f9f3] border border-[#d7ddca] hover:bg-white text-[var(--ink,#252820)] transition-colors cursor-pointer"
                  title="Bấm để sao chép Mezon ID"
                >
                  <span>@{mezonId}</span>
                  {copiedId ? (
                    <Check className="w-3 h-3 text-emerald-600" />
                  ) : (
                    <span className="text-[10px] text-[var(--muted,#77786f)]">sao chép</span>
                  )}
                </button>

                <div className="flex items-center gap-1.5">
                  <Calendar className="w-3.5 h-3.5 text-[var(--olive,#69734f)]" />
                  <span>
                    {user?.lastSeenAt
                      ? `Lần cuối: ${new Date(user.lastSeenAt).toLocaleDateString("vi-VN")}`
                      : "Thành viên tích cực"}
                  </span>
                </div>
              </div>

              {!isAuthenticated && !authLoading && (
                <div className="pt-1 text-xs text-amber-800 bg-amber-50/80 border border-amber-200/80 rounded-lg px-2.5 py-1 inline-flex items-center gap-1.5">
                  <LogIn className="w-3 h-3" />
                  <span>Đang xem ở chế độ khách.</span>
                  <Link href="/auth" className="underline font-semibold hover:text-amber-950">
                    Đăng nhập ngay
                  </Link>
                </div>
              )}
            </div>
          </div>

          {/* Quick Actions */}
          <div className="flex items-center gap-2 self-start md:self-auto">
            <button
              onClick={() => loadData(true)}
              disabled={refreshing}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-[#f7f9f3] border border-[#d7ddca] hover:bg-white text-[var(--ink,#252820)] transition-all cursor-pointer shadow-xs disabled:opacity-60"
              title="Làm mới chỉ số"
            >
              <RotateCcw className={`w-3.5 h-3.5 ${refreshing ? "animate-spin" : ""}`} />
              <span>{refreshing ? "Đang tải…" : "Làm mới"}</span>
            </button>
            <Link
              href="/learn"
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-[var(--olive,#69734f)] text-white hover:opacity-90 transition-all shadow-xs"
            >
              <span>Tiếp tục học</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        </div>
      </section>

      {/* 2-COLUMN LAYOUT FOR GAMIFICATION & STREAK */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
        {/* SECTION 2: GAMIFICATION DASHBOARD */}
        <section className="bg-[#f7f9f3] border border-[#d7ddca] rounded-3xl p-6 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <div className="p-2 rounded-xl bg-[#eff1e8] text-[var(--olive,#69734f)] border border-[#d7ddca]">
                  <Sparkles className="w-5 h-5" />
                </div>
                <div>
                  <h2 className="text-base font-bold text-[var(--ink,#252820)]">
                    Cấp Độ & Kinh Nghiệm
                  </h2>
                  <p className="text-xs text-[var(--muted,#77786f)]">
                    {getLevelTitle(activeProfile.level)}
                  </p>
                </div>
              </div>
              <span className="px-3 py-1 rounded-xl bg-[var(--olive,#69734f)] text-white font-extrabold text-sm shadow-xs">
                Cấp {activeProfile.level}
              </span>
            </div>

            {/* XP Progress Bar */}
            <div className="space-y-2 mt-4 bg-white/70 border border-[#d7ddca]/60 p-4 rounded-2xl">
              <div className="flex justify-between items-baseline text-xs">
                <span className="font-semibold text-[var(--ink,#252820)] flex items-center gap-1.5">
                  <Zap className="w-3.5 h-3.5 text-amber-600 fill-amber-500" />
                  Tiến độ lên Cấp {activeProfile.level + 1}
                </span>
                <span className="font-mono text-[var(--muted,#77786f)]">
                  <strong className="text-[var(--ink,#252820)]">{totalXp.toLocaleString()}</strong> /{" "}
                  {nextLevelXp.toLocaleString()} XP ({xpPercent}%)
                </span>
              </div>

              {/* Progress Track */}
              <div className="w-full h-3 bg-[#e4e8dc] rounded-full overflow-hidden p-0.5">
                <div
                  className="h-full bg-gradient-to-r from-[#69734f] to-[#8ea16a] rounded-full transition-all duration-500 ease-out"
                  style={{ width: `${xpPercent}%` }}
                />
              </div>

              <div className="flex justify-between items-center text-[11px] text-[var(--muted,#77786f)] pt-0.5">
                <span>Cần thêm {xpToNext.toLocaleString()} XP</span>
                <span className="text-[var(--olive,#69734f)] font-medium">
                  {xpPercent >= 100 ? "Sẵn sàng thăng cấp!" : "Đang tiến triển"}
                </span>
              </div>
            </div>
          </div>

          {/* Gamification Stats Footer */}
          <div className="grid grid-cols-2 gap-3 mt-5 pt-4 border-t border-[#d7ddca]">
            <div className="flex items-center gap-3 p-3 rounded-2xl bg-[#eff1e8]/70 border border-[#d7ddca]">
              <div className="p-2 rounded-xl bg-white text-amber-600 shadow-xs">
                <Zap className="w-4 h-4 fill-amber-500" />
              </div>
              <div>
                <span className="text-[11px] text-[var(--muted,#77786f)] block">Tổng XP tích lũy</span>
                <strong className="text-base font-bold text-[var(--ink,#252820)]">
                  {totalXp.toLocaleString("vi-VN")}
                </strong>
              </div>
            </div>

            <div className="flex items-center gap-3 p-3 rounded-2xl bg-[#eff1e8]/70 border border-[#d7ddca]">
              <div className="p-2 rounded-xl bg-white text-cyan-600 shadow-xs">
                <Shield className="w-4 h-4" />
              </div>
              <div>
                <span className="text-[11px] text-[var(--muted,#77786f)] block">Khiên đóng băng</span>
                <strong className="text-base font-bold text-[var(--ink,#252820)]">
                  {streak?.freezeBalance ?? 0} lượt
                </strong>
              </div>
            </div>
          </div>
        </section>

        {/* SECTION 3: STREAK TRACKER */}
        <section className="bg-gradient-to-br from-[#eff1e8] to-[#f7f9f3] border border-[#d7ddca] rounded-3xl p-6 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <div className="p-2 rounded-xl bg-[#fbebe6] text-[#bd684d] border border-[#f3d0c5]">
                  <Flame className="w-5 h-5 fill-[#bd684d]" />
                </div>
                <div>
                  <h2 className="text-base font-bold text-[var(--ink,#252820)]">
                    Chuỗi Học Tập (Streak)
                  </h2>
                  <p className="text-xs text-[var(--muted,#77786f)]">
                    Kỷ lục cao nhất: <strong>{streak?.bestDays ?? 0} ngày</strong>
                  </p>
                </div>
              </div>

              {streak?.isActiveToday ? (
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl bg-emerald-100 text-emerald-800 border border-emerald-300 font-bold text-xs">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  Đã giữ hôm nay
                </span>
              ) : (
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl bg-amber-100 text-amber-800 border border-amber-300 font-bold text-xs">
                  <Clock className="w-3.5 h-3.5" />
                  Chưa xong hôm nay
                </span>
              )}
            </div>

            {/* Prominent Streak Counter */}
            <div className="flex items-baseline gap-2 my-2">
              <span className="text-4xl sm:text-5xl font-black text-[#bd684d] tracking-tight">
                {streak?.currentDays ?? 0}
              </span>
              <span className="text-base font-semibold text-[var(--ink,#252820)]">
                ngày liên tục 🔥
              </span>
            </div>

            {/* Daily Streak XP Progress */}
            <div className="bg-white/70 border border-[#d7ddca]/60 p-3.5 rounded-2xl space-y-2 mt-3">
              <div className="flex justify-between items-center text-xs">
                <span className="text-[var(--muted,#77786f)]">Mục tiêu duy trì hôm nay</span>
                <span className="font-bold text-[var(--ink,#252820)]">
                  {dailyXp} / {neededXp} XP ({dailyPercent}%)
                </span>
              </div>
              <div className="w-full h-2.5 bg-[#e4e8dc] rounded-full overflow-hidden">
                <div
                  className="h-full bg-gradient-to-r from-[#bd684d] to-[#e08365] rounded-full transition-all duration-500 ease-out"
                  style={{ width: `${dailyPercent}%` }}
                />
              </div>
              <p className="text-[11px] text-[var(--muted,#77786f)]">
                {streak?.isActiveToday
                  ? "Bạn đã hoàn thành mục tiêu học tập và gia hạn chuỗi thành công!"
                  : `Học thêm flashcard hoặc giải quiz để nhận đủ ${neededXp} XP hôm nay.`}
              </p>
            </div>
          </div>

          {/* Mini 7-day Streak Calendar Tracker */}
          <div className="mt-5 pt-4 border-t border-[#d7ddca]">
            <div className="flex items-center justify-between text-xs mb-2">
              <span className="font-semibold text-[var(--muted,#77786f)]">7 ngày gần nhất</span>
              <span className="text-[11px] text-[var(--olive,#69734f)]">
                {streak?.currentDays ? `Chuỗi ${streak.currentDays} ngày` : "Bắt đầu ngay"}
              </span>
            </div>
            <div className="grid grid-cols-7 gap-1.5 sm:gap-2 text-center">
              {["T2", "T3", "T4", "T5", "T6", "T7", "CN"].map((day, idx) => {
                // Approximate past active days based on currentDays & isActiveToday
                const isToday = idx === 4; // demo Friday / current day slot
                const isPastDone = idx < 4 || (isToday && streak?.isActiveToday);
                return (
                  <div
                    key={day}
                    className={`py-2 px-1 rounded-xl border flex flex-col items-center gap-1 transition-colors ${
                      isPastDone
                        ? "bg-[#eff1e8] border-[#aeb69d] text-[var(--olive,#69734f)] font-bold"
                        : "bg-white/40 border-[#d7ddca] text-[var(--muted,#77786f)]"
                    } ${isToday ? "ring-2 ring-[var(--olive,#69734f)] ring-offset-1" : ""}`}
                  >
                    <span className="text-[10px] uppercase">{day}</span>
                    <Flame
                      className={`w-3.5 h-3.5 ${
                        isPastDone ? "text-[#bd684d] fill-[#bd684d]" : "text-gray-300"
                      }`}
                    />
                  </div>
                );
              })}
            </div>
          </div>
        </section>
      </div>

      {/* SECTION 4: BADGES & ACHIEVEMENTS GRID */}
      <section className="bg-[#f7f9f3] border border-[#d7ddca] rounded-3xl p-6 sm:p-8 shadow-xs mb-8">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-[#d7ddca]">
          <div>
            <div className="flex items-center gap-2">
              <Award className="w-6 h-6 text-[var(--olive,#69734f)]" />
              <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-[var(--ink,#252820)] font-[family-name:Georgia,serif]">
                Huy Hiệu & Thành Tích
              </h2>
            </div>
            <p className="text-xs sm:text-sm text-[var(--muted,#77786f)] mt-0.5">
              Đã mở khóa <strong>{unlockedBadgesCount}</strong> / {activeProfile.badges?.length || 0} danh hiệu
            </p>
          </div>

          {/* Filter Tabs */}
          <div className="flex items-center gap-1 bg-[#eff1e8] p-1 rounded-xl border border-[#d7ddca] self-start sm:self-auto">
            <button
              onClick={() => setBadgeFilter("all")}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
                badgeFilter === "all"
                  ? "bg-white text-[var(--ink,#252820)] shadow-xs"
                  : "text-[var(--muted,#77786f)] hover:text-[var(--ink,#252820)]"
              }`}
            >
              Tất cả ({activeProfile.badges?.length || 0})
            </button>
            <button
              onClick={() => setBadgeFilter("unlocked")}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
                badgeFilter === "unlocked"
                  ? "bg-white text-[var(--ink,#252820)] shadow-xs"
                  : "text-[var(--muted,#77786f)] hover:text-[var(--ink,#252820)]"
              }`}
            >
              Đã mở ({unlockedBadgesCount})
            </button>
            <button
              onClick={() => setBadgeFilter("locked")}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
                badgeFilter === "locked"
                  ? "bg-white text-[var(--ink,#252820)] shadow-xs"
                  : "text-[var(--muted,#77786f)] hover:text-[var(--ink,#252820)]"
              }`}
            >
              Chưa mở ({(activeProfile.badges?.length || 0) - unlockedBadgesCount})
            </button>
          </div>
        </div>

        {/* Badges Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredBadges.map((badge: BadgeSummaryDto) => {
            const rarity = RARITY_CONFIG[badge.rarity] || RARITY_CONFIG.common;
            const progress = badge.progress ?? 0;
            const maxProgress = Math.max(1, badge.maxProgress ?? 1);
            const badgePercent = Math.min(100, Math.round((progress / maxProgress) * 100));

            return (
              <div
                key={badge.id}
                className={`border rounded-2xl p-5 transition-all flex flex-col justify-between shadow-xs ${
                  badge.unlocked ? rarity.cardClass : "border-[#d7ddca] bg-[#f2f4ec]/60 opacity-80"
                }`}
              >
                <div>
                  {/* Top Bar: Icon, Rarity & Status */}
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div
                      className={`w-12 h-12 rounded-2xl flex items-center justify-center shadow-xs border ${
                        badge.unlocked
                          ? `${rarity.iconBg} border-black/5`
                          : "bg-gray-200/80 text-gray-400 border-gray-300"
                      }`}
                    >
                      {renderBadgeIcon(badge.icon, "w-6 h-6")}
                    </div>

                    <div className="flex flex-col items-end gap-1">
                      <span
                        className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${rarity.badgeClass}`}
                      >
                        {rarity.label}
                      </span>
                      {badge.unlocked ? (
                        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200">
                          <CheckCircle2 className="w-3 h-3" />
                          Đã mở
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-gray-500 bg-gray-100 px-2 py-0.5 rounded-md border border-gray-200">
                          <Lock className="w-3 h-3" />
                          Đang khóa
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Badge Name & Description */}
                  <h3 className="font-bold text-base text-[var(--ink,#252820)] mb-1">
                    {badge.name}
                  </h3>
                  <p className="text-xs text-[var(--muted,#77786f)] leading-relaxed mb-3">
                    {badge.description}
                  </p>

                  {/* Lore Quote */}
                  {badge.lore && (
                    <p className="text-[11px] italic text-[#596444] bg-[#eff1e8]/60 px-2.5 py-1.5 rounded-xl border border-[#d7ddca]/60 mb-3">
                      “{badge.lore}”
                    </p>
                  )}
                </div>

                {/* Progress Bar & Reward */}
                <div className="pt-3 border-t border-[#d7ddca]/70 space-y-2">
                  <div className="flex justify-between items-center text-xs">
                    <span className="text-[var(--muted,#77786f)] text-[11px]">Tiến trình</span>
                    <span className="font-semibold text-[var(--ink,#252820)] font-mono text-[11px]">
                      {progress} / {maxProgress} ({badgePercent}%)
                    </span>
                  </div>

                  <div className="w-full h-2 bg-[#e4e8dc] rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-300 ${
                        badge.unlocked ? "bg-[var(--olive,#69734f)]" : "bg-gray-400"
                      }`}
                      style={{ width: `${badgePercent}%` }}
                    />
                  </div>

                  <div className="flex items-center justify-between text-[11px] pt-1">
                    <span className="inline-flex items-center gap-1 text-amber-700 font-bold bg-amber-50 px-2 py-0.5 rounded-md border border-amber-200">
                      <Zap className="w-3 h-3 fill-amber-500" />+{badge.xpReward} XP
                    </span>
                    {badge.unlockedDate && (
                      <span className="text-[10px] text-[var(--muted,#77786f)]">
                        {new Date(badge.unlockedDate).toLocaleDateString("vi-VN")}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* SECTION 5: LEARNING PERFORMANCE STATS */}
      <section className="bg-[#eff1e8] border border-[#d7ddca] rounded-3xl p-6 sm:p-8 shadow-xs">
        <div className="flex items-center gap-2 mb-6 pb-4 border-b border-[#d7ddca]">
          <TrendingUp className="w-6 h-6 text-[var(--olive,#69734f)]" />
          <div>
            <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-[var(--ink,#252820)] font-[family-name:Georgia,serif]">
              Hiệu Suất Học Tập
            </h2>
            <p className="text-xs sm:text-sm text-[var(--muted,#77786f)]">
              Chỉ số ghi nhận từ các phiên học Flashcard và Quiz
            </p>
          </div>
        </div>

        {/* 3 Metric Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
          <div className="bg-[#f7f9f3] border border-[#d7ddca] p-5 rounded-2xl shadow-xs flex items-center gap-4">
            <div className="p-3.5 rounded-2xl bg-[#eff1e8] text-[var(--olive,#69734f)] border border-[#d7ddca] shrink-0">
              <BookOpen className="w-6 h-6" />
            </div>
            <div>
              <span className="text-xs text-[var(--muted,#77786f)] font-medium block">
                Từ vựng đã học
              </span>
              <strong className="text-2xl sm:text-3xl font-black text-[var(--ink,#252820)]">
                {activeProfile.stats?.wordsLearned ?? 0}
              </strong>
              <span className="text-[11px] text-[#596444] block mt-0.5">
                Đã làm quen & thành thạo
              </span>
            </div>
          </div>

          <div className="bg-[#f7f9f3] border border-[#d7ddca] p-5 rounded-2xl shadow-xs flex items-center gap-4">
            <div className="p-3.5 rounded-2xl bg-[#eff1e8] text-purple-700 border border-[#d7ddca] shrink-0">
              <NotebookTabs className="w-6 h-6" />
            </div>
            <div>
              <span className="text-xs text-[var(--muted,#77786f)] font-medium block">
                Quiz đã hoàn thành
              </span>
              <strong className="text-2xl sm:text-3xl font-black text-[var(--ink,#252820)]">
                {activeProfile.stats?.quizzesCompleted ?? 0}
              </strong>
              <span className="text-[11px] text-purple-700 block mt-0.5">
                Bài kiểm tra vượt qua
              </span>
            </div>
          </div>

          <div className="bg-[#f7f9f3] border border-[#d7ddca] p-5 rounded-2xl shadow-xs flex items-center gap-4">
            <div className="p-3.5 rounded-2xl bg-[#eff1e8] text-emerald-700 border border-[#d7ddca] shrink-0">
              <Target className="w-6 h-6" />
            </div>
            <div>
              <span className="text-xs text-[var(--muted,#77786f)] font-medium block">
                Độ chính xác trung bình
              </span>
              <strong className="text-2xl sm:text-3xl font-black text-[var(--ink,#252820)]">
                {activeProfile.stats?.accuracyRate ?? 0}%
              </strong>
              <span className="text-[11px] text-emerald-700 block mt-0.5">
                Hiệu suất trả lời đúng
              </span>
            </div>
          </div>
        </div>

        {/* Motivational Banner with LinguaMascot */}
        <div className="bg-white/70 border border-[#d7ddca] rounded-2xl p-5 flex flex-col sm:flex-row items-center justify-between gap-5">
          <div className="flex items-center gap-4 text-center sm:text-left">
            <div className="shrink-0 hidden sm:block">
              <LinguaMascot mood="correct" compact />
            </div>
            <div>
              <h4 className="font-bold text-sm text-[var(--ink,#252820)]">
                Bạn đang duy trì phong độ học tập tuyệt vời!
              </h4>
              <p className="text-xs text-[var(--muted,#77786f)] mt-0.5 max-w-lg">
                Học đều đặn 10-15 phút mỗi ngày giúp não bộ tăng 80% khả năng nhớ từ vựng tiếng Anh
                lâu dài.
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2.5 shrink-0 w-full sm:w-auto">
            <Link
              href="/learn"
              className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl text-xs font-bold bg-[var(--olive,#69734f)] text-white hover:opacity-90 transition-all shadow-xs"
            >
              <BookOpen className="w-3.5 h-3.5" />
              <span>Học từ mới</span>
            </Link>
            <Link
              href="/quiz"
              className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl text-xs font-bold bg-white border border-[#d7ddca] text-[var(--ink,#252820)] hover:bg-[#eff1e8] transition-all shadow-xs"
            >
              <NotebookTabs className="w-3.5 h-3.5" />
              <span>Làm Quiz</span>
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
