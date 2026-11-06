"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { BookOpen, Trophy, NotebookTabs, House, Library } from "lucide-react";

const NAV_ITEMS = [
  { href: "/", label: "Tổng quan", icon: House },
  { href: "/learn", label: "Flashcard", icon: BookOpen },
  { href: "/words", label: "Từ vựng", icon: Library },
  { href: "/quiz", label: "Luyện tập", icon: NotebookTabs },
  { href: "/leaderboard", label: "Giải đấu", icon: Trophy },
];

export default function AppNavigation() {
  const pathname = usePathname();
  return (
    <nav className="app-nav" aria-label="Điều hướng chính">
      <Link className="app-brand" href="/" aria-label="LinguaMezon, trang tổng quan">
        <span className="app-brand-mark" aria-hidden="true"><BookOpen /></span>
        <span>Lingua<span className="app-brand-accent">Mezon</span></span>
      </Link>
      <div className="app-nav-items">
        {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link key={href} href={href} className={`app-nav-link ${active ? "is-active" : ""}`} aria-current={active ? "page" : undefined}>
              <Icon aria-hidden="true" />
              <span>{label}</span>
            </Link>
          );
        })}
      </div>
      <span className="app-nav-note">Học theo nhịp độ của bạn</span>
    </nav>
  );
}
