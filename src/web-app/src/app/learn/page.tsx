"use client";

import FlashcardStudy, { SrsReviewPayload } from "@/components/FlashcardStudy";
import { BookOpen } from "lucide-react";

export default function LearnPage() {
  const saveReview = async (payload: SrsReviewPayload) => {
    const response = await fetch("/api/reviews", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(payload) });
    if (!response.ok) throw new Error("Failed to save review");
  };
  return (
    <main className="learn-page">
      <header className="learn-header">
        <div className="learn-context"><BookOpen aria-hidden="true" /><span>Phiên ôn từ vựng</span></div>
      </header>
      <FlashcardStudy onReviewSubmit={saveReview} />
      <footer className="learn-footer"><span>Lặp lại ngắt quãng giúp ghi nhớ lâu hơn.</span><span>SM-2</span></footer>
    </main>
  );
}
