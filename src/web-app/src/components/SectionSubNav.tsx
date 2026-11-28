"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { BookOpen, Gamepad2, Library, Swords } from "lucide-react";
import { useLanguage } from "@/lib/language-context";

export default function SectionSubNav() {
  const pathname = usePathname();
  const { translate: t } = useLanguage();

  const isVocabGroup = pathname === "/words";
  const isArenaGroup = pathname === "/games" || pathname === "/arena";

  if (!isVocabGroup && !isArenaGroup) return null;

  const tabs = isVocabGroup
    ? [
        {
          href: "/words",
          label: t("Thư viện từ vựng", "Vocabulary library"),
          icon: Library,
          active: pathname === "/words",
        },
        {
          href: "/learn",
          label: t("Ôn Flashcard SM-2", "SM-2 Flashcards"),
          icon: BookOpen,
          active: false,
        },
      ]
    : [
        {
          href: "/games",
          label: t("Game Center (6 mini-game)", "Game Center (6 mini-games)"),
          icon: Gamepad2,
          active: pathname === "/games",
        },
        {
          href: "/arena",
          label: t("Đấu trường 1vs1", "1v1 Arena"),
          icon: Swords,
          active: pathname === "/arena",
        },
      ];

  return (
    <div className="section-subnav-wrap">
      <nav
        className="section-subnav"
        data-tour={isArenaGroup ? "games-subnav" : "vocab-subnav"}
        aria-label={
          isVocabGroup
            ? t("Chuyển nhanh mục Học từ", "Vocabulary section switcher")
            : t("Chuyển nhanh mục Đấu trường", "Arena section switcher")
        }
      >
        {tabs.map(({ href, label, icon: Icon, active }) => (
          <Link
            key={href}
            href={href}
            className={`section-subnav-tab ${active ? "is-active" : ""}`}
            aria-current={active ? "page" : undefined}
          >
            <Icon size={15} aria-hidden="true" />
            <span>{label}</span>
          </Link>
        ))}
      </nav>
    </div>
  );
}
