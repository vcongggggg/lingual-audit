"use client";

import FlashcardStudy, { SrsReviewPayload } from "@/components/FlashcardStudy";
import { BookOpen } from "lucide-react";

export default function LearnPage() {
  const saveReview = async (payload: SrsReviewPayload) => {
    const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
    const headers: Record<string, string> = { "content-type": "application/json" };
    if (token) {
      headers["authorization"] = `Bearer ${token}`;
    }

    const response = await fetch("/api/reviews", {
      method: "POST",
      headers,
      body: JSON.stringify({
        vocabularyId: payload.vocabulary_id,
        rating: payload.rating,
        quality: payload.quality,
        repetitions: payload.repetitions,
        intervalDays: payload.interval_days,
        easeFactor: payload.ease_factor,
        nextDueAt: payload.next_due_at,
        timeSpentMs: payload.time_spent_ms,
        idempotencyKey: payload.idempotency_key,
      }),
    });
    if (!response.ok) throw new Error(`Failed to save review (HTTP ${response.status})`);
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
