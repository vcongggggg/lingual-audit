"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  BookOpen,
  Trophy,
  NotebookTabs,
  House,
  Bot,
  Languages,
  Gamepad2,
  LogIn,
  LogOut,
  HelpCircle,
} from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { useAuth } from "@/lib/auth-context";
import { triggerOnboardingTour } from "@/components/OnboardingTour";

const NAV_ITEMS = [
  { href: "/", matchPaths: ["/"], labels: { vi: "Tổng quan", en: "Overview" }, icon: House },
  { href: "/words", matchPaths: ["/words", "/learn"], labels: { vi: "Học từ", en: "Vocabulary" }, icon: BookOpen },
  { href: "/quiz", matchPaths: ["/quiz"], labels: { vi: "Luyện tập", en: "Practice" }, icon: NotebookTabs },
  { href: "/games", matchPaths: ["/games", "/arena"], labels: { vi: "Đấu trường", en: "Arena" }, icon: Gamepad2 },
  { href: "/tutor", matchPaths: ["/tutor"], labels: { vi: "LingLing AI", en: "LingLing AI" }, icon: Bot },
  { href: "/leaderboard", matchPaths: ["/leaderboard"], labels: { vi: "Giải đấu", en: "Leaderboard" }, icon: Trophy },
];

export default function AppNavigation() {
  const pathname = usePathname();
  const { language, toggleLanguage, dictionary, translate: t } = useLanguage();
  const { user, isAuthenticated, loading, logout } = useAuth();

  return (
    <nav className="app-nav" aria-label={dictionary.navigationLabel}>
      <Link className="app-brand" href="/" aria-label={language === "vi" ? "LinguaMezon, trang tổng quan" : "LinguaMezon, overview page"}>
        <span className="app-brand-mark" aria-hidden="true"><BookOpen /></span>
        <span>Lingua<span className="app-brand-accent">Mezon</span></span>
      </Link>

      {/* Main Navigation Links (6 compact primary tabs) */}
      <div className="app-nav-items">
        {NAV_ITEMS.map(({ href, matchPaths, labels, icon: Icon }) => {
          const active = matchPaths.includes(pathname);
          return (
            <Link
              key={href}
              href={href}
              className={`app-nav-link ${active ? "is-active" : ""}`}
              aria-current={active ? "page" : undefined}
            >
              <Icon aria-hidden="true" />
              <span>{labels[language]}</span>
            </Link>
          );
        })}
      </div>

      <div className="app-nav-actions">
        <button
          className="app-help-toggle"
          type="button"
          onClick={triggerOnboardingTour}
          aria-label={t("Mở hướng dẫn sử dụng", "Open user guide")}
          title={t("Hướng dẫn sử dụng", "User guide")}
        >
          <HelpCircle size={15} aria-hidden="true" />
        </button>
        <button
          className="app-language-toggle"
          type="button"
          onClick={toggleLanguage}
          aria-label={dictionary.navigationAriaLabel}
          title={dictionary.languageName}
        >
          <Languages size={15} aria-hidden="true" />
          <span className={language === "vi" ? "is-selected" : ""}>VI</span>
          <span aria-hidden="true">/</span>
          <span className={language === "en" ? "is-selected" : ""}>EN</span>
        </button>
        <span className="app-nav-note">{dictionary.navigationNote}</span>
      </div>

      {/* Auth Status & User Profile Pill */}
      <div className="app-nav-auth flex items-center gap-2 shrink-0">
        {!loading && (
          isAuthenticated && user ? (
            <div className="flex items-center gap-1.5 sm:gap-2 bg-[#eff1e8] border border-[#d7ddca] pl-1.5 pr-1.5 sm:pr-2 py-1 rounded-xl shadow-xs shrink-0">
              <Link
                href="/profile"
                className="flex items-center gap-2 hover:opacity-85 transition-opacity shrink-0"
                title={t("Xem hồ sơ cá nhân", "View profile")}
              >
                {user.avatarUrl && user.avatarUrl.trim() !== "" ? (
                  <img
                    src={user.avatarUrl}
                    alt={user.displayName}
                    className="w-6 h-6 rounded-lg object-cover border border-[#d7ddca]"
                  />
                ) : (
                  <div className="w-6 h-6 rounded-lg bg-[var(--olive,#69734f)] text-white flex items-center justify-center font-bold text-[11px] shrink-0">
                    {user.displayName?.charAt(0).toUpperCase() || "U"}
                  </div>
                )}
                <div className="hidden lg:flex flex-col text-left">
                  <span className="text-xs font-semibold text-[var(--ink,#252820)] max-w-[116px] truncate leading-tight">
                    {user.displayName}
                  </span>
                  <span className="text-[10px] text-[var(--olive,#69734f)] font-medium leading-tight">
                    {user.role === "admin"
                      ? t("Quản trị", "Admin")
                      : t("Học viên", "Learner")}
                  </span>
                </div>
              </Link>
              <button
                onClick={logout}
                title={t("Đăng xuất", "Sign out")}
                className="p-1 hover:bg-[#e0e4d5] rounded-md text-[var(--muted,#77786f)] hover:text-red-600 transition-colors cursor-pointer shrink-0"
                aria-label={t("Đăng xuất", "Sign out")}
              >
                <LogOut className="w-3.5 h-3.5" />
              </button>
            </div>
          ) : (
            <Link
              href="/auth"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl bg-[var(--olive,#69734f)] hover:opacity-90 text-white transition-all shadow-xs whitespace-nowrap shrink-0"
            >
              <LogIn className="w-3.5 h-3.5 shrink-0" />
              <span>{t("Đăng nhập", "Sign in")}</span>
            </Link>
          )
        )}
      </div>
    </nav>
  );
}
