"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { BookOpen, Trophy, NotebookTabs, House, Library, Bot, Languages, Swords, Gamepad2, LogIn, LogOut, User } from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { useAuth } from "@/lib/auth-context";

const NAV_ITEMS = [
  { href: "/", labels: { vi: "Tổng quan", en: "Overview" }, icon: House },
  { href: "/learn", labels: { vi: "Flashcard", en: "Flashcards" }, icon: BookOpen },
  { href: "/words", labels: { vi: "Từ vựng", en: "Vocabulary" }, icon: Library },
  { href: "/quiz", labels: { vi: "Luyện tập", en: "Practice" }, icon: NotebookTabs },
  { href: "/games", labels: { vi: "Game Center", en: "Game Center" }, icon: Gamepad2 },
  { href: "/tutor", labels: { vi: "LingLing AI", en: "LingLing AI" }, icon: Bot },
  { href: "/leaderboard", labels: { vi: "Giải đấu", en: "Leaderboard" }, icon: Trophy },
  { href: "/arena", labels: { vi: "Arena 1vs1", en: "1v1 Arena" }, icon: Swords },
];

export default function AppNavigation() {
  const pathname = usePathname();
  const { language, toggleLanguage, dictionary } = useLanguage();
  const { user, isAuthenticated, loading, logout } = useAuth();

  return (
    <nav className="app-nav" aria-label={dictionary.navigationLabel}>
      <Link className="app-brand" href="/" aria-label={language === "vi" ? "LinguaMezon, trang tổng quan" : "LinguaMezon, overview page"}>
        <span className="app-brand-mark" aria-hidden="true"><BookOpen /></span>
        <span>Lingua<span className="app-brand-accent">Mezon</span></span>
      </Link>

      {/* Main Navigation Links */}
      <div className="app-nav-items">
        {NAV_ITEMS.map(({ href, labels, icon: Icon }) => {
          const active = pathname === href;
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
      <div className="app-nav-auth flex items-center gap-2">
        {!loading && (
          isAuthenticated && user ? (
            <div className="flex items-center gap-2 bg-[#eff1e8] border border-[#d7ddca] pl-1.5 pr-2 py-1 rounded-xl shadow-xs">
              <Link
                href="/profile"
                className="flex items-center gap-2 hover:opacity-85 transition-opacity"
                title="Xem hồ sơ cá nhân"
              >
                {user.avatarUrl && user.avatarUrl.trim() !== "" ? (
                  <img
                    src={user.avatarUrl}
                    alt={user.displayName}
                    className="w-6 h-6 rounded-lg object-cover border border-[#d7ddca]"
                  />
                ) : (
                  <div className="w-6 h-6 rounded-lg bg-[var(--olive,#69734f)] text-white flex items-center justify-center font-bold text-[11px]">
                    {user.displayName?.charAt(0).toUpperCase() || "U"}
                  </div>
                )}
                <div className="hidden sm:flex flex-col text-left">
                  <span className="text-xs font-semibold text-[var(--ink,#252820)] max-w-[120px] truncate leading-tight">
                    {user.displayName}
                  </span>
                  <span className="text-[10px] text-[var(--olive,#69734f)] font-medium leading-tight">
                    {user.role === "admin" ? "Quản trị" : "Học viên"}
                  </span>
                </div>
              </Link>
              <button
                onClick={logout}
                title="Đăng xuất"
                className="p-1 hover:bg-[#e0e4d5] rounded-md text-[var(--muted,#77786f)] hover:text-red-600 transition-colors cursor-pointer ml-1"
                aria-label="Đăng xuất"
              >
                <LogOut className="w-3.5 h-3.5" />
              </button>
            </div>
          ) : (
            <Link
              href="/auth"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl bg-[var(--olive,#69734f)] hover:opacity-90 text-white transition-all shadow-xs"
            >
              <LogIn className="w-3.5 h-3.5" />
              <span>Đăng nhập</span>
            </Link>
          )
        )}
      </div>
    </nav>
  );
}
