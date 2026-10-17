"use client";

import FlashcardStudy from "@/components/FlashcardStudy";
import { BookOpen } from "lucide-react";

export default function LearnPage() {
  return (
    <main className="learn-page">
      <header className="learn-header">
        <div className="learn-context"><BookOpen aria-hidden="true" /><span>Phiên ôn từ vựng</span></div>
      </header>
      <FlashcardStudy />
      <footer className="learn-footer"><span>Lặp lại ngắt quãng giúp ghi nhớ lâu hơn.</span><span>SM-2</span></footer>
    </main>
  );
}
