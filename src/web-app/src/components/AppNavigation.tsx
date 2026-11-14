"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { BookOpen, Trophy, NotebookTabs, House, Library, Bot, Languages, Swords } from "lucide-react";
import { useLanguage } from "@/lib/language-context";

const NAV_ITEMS = [
  { href: "/", labels: { vi: "Tổng quan", en: "Overview" }, icon: House },
  { href: "/learn", labels: { vi: "Flashcard", en: "Flashcards" }, icon: BookOpen },
  { href: "/words", labels: { vi: "Từ vựng", en: "Vocabulary" }, icon: Library },
  { href: "/quiz", labels: { vi: "Luyện tập", en: "Practice" }, icon: NotebookTabs },
  { href: "/tutor", labels: { vi: "LingLing AI", en: "LingLing AI" }, icon: Bot },
  { href: "/leaderboard", labels: { vi: "Giải đấu", en: "Leaderboard" }, icon: Trophy },
  { href: "/arena", labels: { vi: "Arena 1vs1", en: "1v1 Arena" }, icon: Swords },
];

export default function AppNavigation() {
  const pathname = usePathname();
  const { language, toggleLanguage, dictionary } = useLanguage();

  return (
    <nav className="app-nav" aria-label={dictionary.navigationLabel}>
      <Link className="app-brand" href="/" aria-label="LinguaMezon, trang tổng quan">
        <span className="app-brand-mark" aria-hidden="true"><BookOpen /></span>
        <span>Lingua<span className="app-brand-accent">Mezon</span></span>
      </Link>
      <div className="app-nav-items">
        {NAV_ITEMS.map(({ href, labels, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link key={href} href={href} className={`app-nav-link ${active ? "is-active" : ""}`} aria-current={active ? "page" : undefined}>
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
    </nav>
  );
}
