"use client";

import { useCallback, useState } from "react";
import Link from "next/link";
import FlashcardStudy, {
  type FlashcardProgressSnapshot,
  type SrsReviewPayload,
} from "@/components/FlashcardStudy";
import { BookOpen, Library, Zap } from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { clientApi } from "@/app/api/api";

const CEFR_OPTIONS = ["ALL", "A1", "A2", "B1", "B2", "C1", "C2"] as const;
const LIMIT_OPTIONS = [10, 20, 50] as const;

export default function LearnPage() {
  const { translate: t } = useLanguage();
  const [selectedLevel, setSelectedLevel] = useState<string>("ALL");
  const [deckLimit, setDeckLimit] = useState<number>(20);
  const [progress, setProgress] = useState<FlashcardProgressSnapshot>({
    current: 0,
    total: 0,
    percent: 0,
    xpEarned: 0,
    reviewedCount: 0,
  });

  const handleProgressChange = useCallback((snapshot: FlashcardProgressSnapshot) => {
    setProgress(snapshot);
  }, []);

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
    <main className="learn-page learn-cockpit-page">
      {/* Compact single-row Top Control Bar on Desktop */}
      <header className="learn-top-bar">
        <div className="learn-subtabs" role="navigation" aria-label={t("Chuyển nhanh mục Học từ", "Vocabulary switcher")}>
          <Link href="/words" className="learn-subtab">
            <Library size={14} aria-hidden="true" />
            <span>{t("Thư viện từ vựng", "Vocabulary library")}</span>
          </Link>
          <Link href="/learn" className="learn-subtab is-active" aria-current="page">
            <BookOpen size={14} aria-hidden="true" />
            <span>{t("Ôn Flashcard SM-2", "SM-2 Flashcards")}</span>
          </Link>
        </div>

        <div className="learn-filters" data-tour="learn-filters" aria-label={t("Lọc bộ thẻ", "Deck filters")}>
          <div className="learn-cefr-pills" role="group" aria-label={t("Cấp độ CEFR", "CEFR level")}>
            {CEFR_OPTIONS.map((lvl) => (
              <button
                key={lvl}
                type="button"
                className={`learn-pill ${selectedLevel === lvl ? "is-active" : ""}`}
                onClick={() => setSelectedLevel(lvl)}
                aria-pressed={selectedLevel === lvl}
              >
                {lvl === "ALL" ? t("Tất cả", "All") : lvl}
              </button>
            ))}
          </div>

          <div className="learn-limit-pills" role="group" aria-label={t("Số thẻ phiên học", "Session card count")}>
            {LIMIT_OPTIONS.map((count) => (
              <button
                key={count}
                type="button"
                className={`learn-pill ${deckLimit === count ? "is-active" : ""}`}
                onClick={() => setDeckLimit(count)}
                aria-pressed={deckLimit === count}
              >
                {count} {t("thẻ", "cards")}
              </button>
            ))}
          </div>
        </div>

        <div className="learn-progress-pill" aria-live="polite">
          <span>
            {t("Thẻ", "Card")} <strong>{progress.current}</strong>/{progress.total}
          </span>
          <span className="learn-progress-sep">·</span>
          <span className="learn-progress-pct">{progress.percent}%</span>
          <span className="learn-progress-sep">·</span>
          <span className="learn-progress-xp">
            <Zap size={12} aria-hidden="true" /> +{progress.xpEarned} XP
          </span>
        </div>
      </header>

      <div
        className="learn-top-progress-track"
        role="progressbar"
        aria-label={t("Tiến độ phiên học", "Session progress")}
        aria-valuemin={0}
        aria-valuemax={progress.total}
        aria-valuenow={progress.current}
      >
        <span style={{ width: `${progress.percent}%` }} />
      </div>

      <FlashcardStudy
        levelFilter={selectedLevel}
        deckLimit={deckLimit}
        onProgressChange={handleProgressChange}
        onReviewSubmit={saveReview}
      />

      <footer className="learn-footer">
        <span>{t("Lặp lại ngắt quãng giúp ghi nhớ lâu hơn.", "Spaced repetition helps you remember for longer.")}</span>
        <span>SM-2</span>
      </footer>
    </main>
  );
}
