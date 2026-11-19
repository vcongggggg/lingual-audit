"use client";

import FlashcardStudy, { SrsReviewPayload } from "@/components/FlashcardStudy";
import { BookOpen } from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { clientApi } from "@/app/api/api";

export default function LearnPage() {
  const { translate: t } = useLanguage();
  const saveReview = async (payload: SrsReviewPayload) => {
    await clientApi.post("/api/reviews", {
      vocabularyId: payload.vocabulary_id,
      rating: payload.rating,
      quality: payload.quality,
      repetitions: payload.repetitions,
      intervalDays: payload.interval_days,
      easeFactor: payload.ease_factor,
      nextDueAt: payload.next_due_at,
      timeSpentMs: payload.time_spent_ms,
      idempotencyKey: payload.idempotency_key,
    });
  };

  return (
    <main className="learn-page">
      <header className="learn-header">
        <div className="learn-context"><BookOpen aria-hidden="true" /><span>{t("Phiên ôn từ vựng", "Vocabulary review session")}</span></div>
      </header>
      <FlashcardStudy onReviewSubmit={saveReview} />
      <footer className="learn-footer"><span>{t("Lặp lại ngắt quãng giúp ghi nhớ lâu hơn.", "Spaced repetition helps you remember for longer.")}</span><span>SM-2</span></footer>
    </main>
  );
}
