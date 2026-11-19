"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { BookOpen, Trophy, NotebookTabs, House, Library, Bot, Swords, LogIn, LogOut } from "lucide-react";
import { useAuth } from "@/lib/auth-context";

const NAV_ITEMS = [
  { href: "/", label: "Tổng quan", icon: House },
  { href: "/learn", label: "Flashcard", icon: BookOpen },
  { href: "/words", label: "Từ vựng", icon: Library },
  { href: "/quiz", label: "Luyện tập", icon: NotebookTabs },
  { href: "/tutor", label: "LingLing AI", icon: Bot },
  { href: "/leaderboard", label: "Giải đấu", icon: Trophy },
  { href: "/arena", label: "Arena 1vs1", icon: Swords },
];

export default function AppNavigation() {
  const pathname = usePathname();
  const { user, isAuthenticated, logout, loading } = useAuth();

  return (
    <nav className="app-nav" aria-label="Điều hướng chính">
      {/* Brand Identity */}
      <Link className="app-brand" href="/" aria-label="LinguaMezon, trang tổng quan">
        <span className="app-brand-mark" aria-hidden="true"><BookOpen /></span>
        <span>Lingua<span className="app-brand-accent">Mezon</span></span>
      </Link>

      {/* Main Navigation Links */}
      <div className="app-nav-items">
        {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className={`app-nav-link ${active ? "is-active" : ""}`}
              aria-current={active ? "page" : undefined}
            >
              <Icon aria-hidden="true" />
              <span>{label}</span>
            </Link>
          );
        })}
      </div>

      {/* Auth Status & User Profile Pill */}
      <div className="app-nav-auth flex items-center gap-2">
        {!loading && (
          isAuthenticated && user ? (
            <div className="flex items-center gap-2 bg-[#eff1e8] border border-[#d7ddca] pl-1.5 pr-2 py-1 rounded-xl shadow-xs">
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
