"use client";

import React, { useState, useEffect, useCallback, useMemo, useRef } from "react";
import Link from "next/link";
import { clientApi } from "@/app/api/api";
import {
  Volume2,
  VolumeX,
  RotateCw,
  Zap,
  ArrowRight,
  BookOpen,
  Keyboard,
  RefreshCw,
} from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { useSoundEffects } from "@/lib/sound-effects";

// ============================================================================
// 1. DATA TYPES (MAPPED TO BACKEND POSTGRESQL SCHEMA: vocabulary_items & srs_cards)
// ============================================================================

export type Sm2Rating = "again" | "hard" | "good" | "easy";

export interface VocabularyExample {
  source_text: string;
  target_text: string;
  audio_url?: string;
}

export interface VocabularyItem {
  id: string;
  term: string;
  normalized_term: string;
  source_language: string;
  target_language: string;
  ipa?: string;
  cefr_level?: "A1" | "A2" | "B1" | "B2" | "C1" | "C2";
  part_of_speech?: string;
  meaning: string;
  pronunciation_audio_url?: string;
  image_url?: string;
  examples: VocabularyExample[];
  collocations?: string[];
}

export interface SrsCardState {
  user_id: string;
  vocabulary_id: string;
  repetitions: number;
  interval_days: number;
  ease_factor: number;
  next_due_at: string;
  last_reviewed_at?: string;
  lapses: number;
}

export interface Sm2CalculationResult {
  rating: Sm2Rating;
  quality: number;
  repetitions: number;
  interval_days: number;
  ease_factor: number;
  next_due_at: Date;
  interval_label: string;
}

export interface SrsReviewPayload {
  vocabulary_id: string;
  rating: Sm2Rating;
  quality: number;
  repetitions: number;
  interval_days: number;
  ease_factor: number;
  next_due_at: string;
  time_spent_ms: number;
  idempotency_key: string;
}

// ============================================================================
// 2. SM-2 ALGORITHM CLIENT HELPER (SUPERMEMO-2 STANDARD - PRESERVED)
// ============================================================================

export function calculateSm2(
  currentState: { repetitions: number; interval_days: number; ease_factor: number },
  rating: Sm2Rating
): Sm2CalculationResult {
  const qualityMap: Record<Sm2Rating, number> = {
    again: 1,
    hard: 2,
    good: 4,
    easy: 5,
  };

  const q = qualityMap[rating];
  let { repetitions, interval_days } = currentState;
  const ease_factor = currentState.ease_factor;

  if (q < 3) {
    repetitions = 0;
    interval_days = 0.007;
  } else {
    if (repetitions === 0) {
      interval_days = 1;
    } else if (repetitions === 1) {
      interval_days = rating === "hard" ? 3 : 6;
    } else {
      let multiplier = ease_factor;
      if (rating === "hard") multiplier = 1.2;
      if (rating === "easy") multiplier = ease_factor * 1.3;
      interval_days = Math.round(interval_days * multiplier * 10) / 10;
    }
    repetitions += 1;
  }

  let newEaseFactor = ease_factor + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02));
  if (newEaseFactor < 1.3) newEaseFactor = 1.3;
  newEaseFactor = Math.round(newEaseFactor * 100) / 100;

  const nextDueDate = new Date(Date.now() + interval_days * 24 * 60 * 60 * 1000);

  let intervalLabel = "";
  if (interval_days < 0.02) {
    intervalLabel = "< 10 phút";
  } else if (interval_days <= 1) {
    intervalLabel = "1 ngày";
  } else {
    intervalLabel = `${Math.round(interval_days)} ngày`;
  }

  return {
    rating,
    quality: q,
    repetitions,
    interval_days,
    ease_factor: newEaseFactor,
    next_due_at: nextDueDate,
    interval_label: intervalLabel,
  };
}

// ============================================================================
// 3. MAIN 3D FLASHCARD SPLIT 2-COLUMN COCKPIT COMPONENT
// ============================================================================

export interface FlashcardProgressSnapshot {
  current: number;
  total: number;
  percent: number;
  xpEarned: number;
  reviewedCount: number;
}

interface FlashcardStudyProps {
  vocabularyList?: VocabularyItem[];
  levelFilter?: string;
  deckLimit?: number;
  onProgressChange?: (snapshot: FlashcardProgressSnapshot) => void;
  onFinishSession?: (stats: { totalReviewed: number; xpEarned: number }) => void;
  onReviewSubmit?: (payload: SrsReviewPayload) => Promise<void> | void;
}

export default function FlashcardStudy({
  vocabularyList,
  levelFilter = "ALL",
  deckLimit = 20,
  onProgressChange,
  onFinishSession,
  onReviewSubmit,
}: FlashcardStudyProps) {
  const { translate: t, language } = useLanguage();
  const { sfxEnabled, toggleSound, playFlip, playCorrect, playWrong, playVictory } = useSoundEffects();

  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [speechUnavailable, setSpeechUnavailable] = useState(false);
  const [xpGainedSession, setXpGainedSession] = useState(0);
  const [recentXpPopup, setRecentXpPopup] = useState<number | null>(null);
  const [reviewedCardsCount, setReviewedCardsCount] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [reviewError, setReviewError] = useState<string | null>(null);
  const [startTime, setStartTime] = useState<number>(() => Date.now());
  const [isCompleted, setIsCompleted] = useState(false);
  const [databaseVocabulary, setDatabaseVocabulary] = useState<VocabularyItem[]>([]);
  const reviewIdempotencyKeys = useRef<Record<string, string>>({});
  const [isLoadingVocabulary, setIsLoadingVocabulary] = useState(!vocabularyList);
  const [vocabularyLoadError, setVocabularyLoadError] = useState<string | null>(null);
  const [reloadNonce, setReloadNonce] = useState(0);
  const [srsDeckState, setSrsDeckState] = useState<Record<string, SrsCardState>>({});

  useEffect(() => {
    if (vocabularyList) return;
    const controller = new AbortController();
    const loadVocabulary = async () => {
      setIsLoadingVocabulary(true);
      setVocabularyLoadError(null);
      setIsCompleted(false);
      setIsFlipped(false);
      setCurrentIndex(0);
      setReviewedCardsCount(0);
      setXpGainedSession(0);
      setStartTime(Date.now());
      try {
        const params = new URLSearchParams({ limit: String(deckLimit), mode: "study" });
        if (levelFilter && levelFilter !== "ALL") {
          params.set("level", levelFilter);
        }
        const data = await clientApi.get<{
          words?: (VocabularyItem & {
            repetitions?: number;
            interval_days?: number;
            ease_factor?: number;
            lapses?: number;
            next_due_at?: string;
          })[];
        }>(`/api/vocabulary?${params.toString()}`, {
          signal: controller.signal,
        });
        if (Array.isArray(data?.words) && data.words.length > 0) {
          setDatabaseVocabulary(data.words);
          const hydratedSrs: Record<string, SrsCardState> = {};
          for (const w of data.words) {
            if (typeof w.repetitions === "number" && w.repetitions > 0) {
              hydratedSrs[w.id] = {
                user_id: "user-current",
                vocabulary_id: w.id,
                repetitions: w.repetitions,
                interval_days: typeof w.interval_days === "number" ? w.interval_days : 1,
                ease_factor: typeof w.ease_factor === "number" ? w.ease_factor : 2.5,
                next_due_at: w.next_due_at ?? "",
                lapses: typeof w.lapses === "number" ? w.lapses : 0,
              };
            }
          }
          setSrsDeckState(hydratedSrs);
          setVocabularyLoadError(null);
        } else {
          setDatabaseVocabulary([]);
          setVocabularyLoadError(
            t("Không có từ vựng phù hợp với bộ lọc hiện tại.", "No vocabulary words match the selected filter.")
          );
        }
      } catch (error) {
        if (controller.signal.aborted) return;
        console.warn("Could not load vocabulary from database:", error);
        setDatabaseVocabulary([]);
        setVocabularyLoadError(
          t("Chưa kết nối được kho từ vựng. Vui lòng thử tải lại.", "Could not connect to the vocabulary library. Please try reloading.")
        );
      } finally {
        if (!controller.signal.aborted) setIsLoadingVocabulary(false);
      }
    };
    void loadVocabulary();
    return () => controller.abort();
  }, [vocabularyList, levelFilter, deckLimit, reloadNonce, t]);

  const cards = vocabularyList ?? databaseVocabulary;

  const currentVocab = cards[currentIndex] || cards[0] || {
    id: "",
    term: "",
    normalized_term: "",
    source_language: "en",
    target_language: "vi",
    meaning: "",
    examples: [],
  };

  const currentCardSrs = useMemo(
    () =>
      srsDeckState[currentVocab.id] || {
        user_id: "user-current",
        vocabulary_id: currentVocab.id,
        repetitions: 0,
        interval_days: 0,
        ease_factor: 2.5,
        next_due_at: "",
        lapses: 0,
      },
    [currentVocab.id, srsDeckState]
  );

  const formatIntervalLabel = useCallback(
    (intervalDays: number) => {
      if (intervalDays < 0.02) return language === "vi" ? "< 10 phút" : "< 10m";
      if (intervalDays <= 1) return language === "vi" ? "1 ngày" : "1 day";
      const days = Math.round(intervalDays);
      return language === "vi" ? `${days} ngày` : `${days} days`;
    },
    [language]
  );

  const buttonIntervals = useMemo(
    () => ({
      again: formatIntervalLabel(calculateSm2(currentCardSrs, "again").interval_days),
      hard: formatIntervalLabel(calculateSm2(currentCardSrs, "hard").interval_days),
      good: formatIntervalLabel(calculateSm2(currentCardSrs, "good").interval_days),
      easy: formatIntervalLabel(calculateSm2(currentCardSrs, "easy").interval_days),
    }),
    [currentCardSrs, formatIntervalLabel]
  );

  const progressPercent = cards.length ? Math.round(((currentIndex + 1) / cards.length) * 100) : 0;

  useEffect(() => {
    onProgressChange?.({
      current: cards.length ? currentIndex + 1 : 0,
      total: cards.length,
      percent: progressPercent,
      xpEarned: xpGainedSession,
      reviewedCount: reviewedCardsCount,
    });
  }, [cards.length, currentIndex, onProgressChange, progressPercent, reviewedCardsCount, xpGainedSession]);

  const handlePronounce = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation();
      if (typeof window === "undefined" || !("speechSynthesis" in window)) {
        setSpeechUnavailable(true);
        return;
      }
      setSpeechUnavailable(false);

      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(currentVocab.term);
      utterance.lang = "en-US";
      utterance.rate = 0.88;
      utterance.pitch = 1.0;

      utterance.onstart = () => setIsSpeaking(true);
      utterance.onend = () => setIsSpeaking(false);
      utterance.onerror = () => setIsSpeaking(false);

      window.speechSynthesis.speak(utterance);
    },
    [currentVocab.term]
  );

  const handleToggleFlip = useCallback(() => {
    if (isSubmitting) return;
    playFlip();
    setIsFlipped((prev) => !prev);
  }, [isSubmitting, playFlip]);

  const handleSm2Review = useCallback(
    async (rating: Sm2Rating) => {
      if (isSubmitting) return;
      setIsSubmitting(true);
      setReviewError(null);

      const timeSpentMs = Date.now() - startTime;
      const sm2Result = calculateSm2(currentCardSrs, rating);

      const xpMap: Record<Sm2Rating, number> = {
        again: 5,
        hard: 15,
        good: 20,
        easy: 25,
      };
      const earnedXp = xpMap[rating];

      const payload: SrsReviewPayload = {
        vocabulary_id: currentVocab.id,
        rating,
        quality: sm2Result.quality,
        repetitions: sm2Result.repetitions,
        interval_days: sm2Result.interval_days,
        ease_factor: sm2Result.ease_factor,
        next_due_at: sm2Result.next_due_at.toISOString(),
        time_spent_ms: timeSpentMs,
        idempotency_key:
          reviewIdempotencyKeys.current[`${currentVocab.id}:${rating}`] ??
          (reviewIdempotencyKeys.current[`${currentVocab.id}:${rating}`] = `${currentVocab.id}-${rating}-${crypto.randomUUID()}`),
      };

      if (onReviewSubmit) {
        try {
          await onReviewSubmit(payload);
          delete reviewIdempotencyKeys.current[`${currentVocab.id}:${rating}`];
        } catch (err) {
          console.error("Lỗi khi gửi kết quả SRS lên server:", err);
          const status = typeof err === "object" && err !== null && "status" in err ? Number((err as { status?: unknown }).status) : null;
          const rawMsg = err instanceof Error ? err.message : "";
          const isAuthError = status === 401 || /401|unauthoriz|sign in|đăng nhập/i.test(rawMsg);
          setReviewError(
            isAuthError
              ? t("Chưa lưu được kết quả (hãy đăng nhập Mezon để lưu tiến độ).", "Could not save review (sign in with Mezon to save progress).")
              : rawMsg
                ? t(`Chưa lưu được kết quả: ${rawMsg}`, `Could not save review: ${rawMsg}`)
                : t("Chưa lưu được kết quả do lỗi máy chủ. Vui lòng thử lại.", "Could not save review due to a server error. Please try again.")
          );
          playWrong();
          setIsSubmitting(false);
          return;
        }
      }

      if (rating === "again") {
        playWrong();
      } else {
        playCorrect(rating === "easy" ? 2 : 1);
      }

      setSrsDeckState((prev) => ({
        ...prev,
        [currentVocab.id]: {
          ...currentCardSrs,
          repetitions: sm2Result.repetitions,
          interval_days: sm2Result.interval_days,
          ease_factor: sm2Result.ease_factor,
          next_due_at: sm2Result.next_due_at.toISOString(),
          last_reviewed_at: new Date().toISOString(),
          lapses: rating === "again" ? currentCardSrs.lapses + 1 : currentCardSrs.lapses,
        },
      }));

      setRecentXpPopup(earnedXp);
      setXpGainedSession((prev) => prev + earnedXp);
      setReviewedCardsCount((prev) => prev + 1);

      setTimeout(() => {
        setRecentXpPopup(null);
        setIsFlipped(false);
        setIsSubmitting(false);
        setStartTime(Date.now());

        if (currentIndex + 1 < cards.length) {
          setCurrentIndex((prev) => prev + 1);
        } else {
          playVictory();
          setIsCompleted(true);
          if (onFinishSession) {
            onFinishSession({
              totalReviewed: reviewedCardsCount + 1,
              xpEarned: xpGainedSession + earnedXp,
            });
          }
        }
      }, 360);
    },
    [
      currentCardSrs,
      currentIndex,
      currentVocab.id,
      isSubmitting,
      onFinishSession,
      onReviewSubmit,
      playCorrect,
      playVictory,
      playWrong,
      reviewedCardsCount,
      startTime,
      cards.length,
      t,
      xpGainedSession,
    ]
  );

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (isCompleted) return;
      if (
        e.repeat ||
        (e.target instanceof HTMLElement &&
          (e.target.isContentEditable ||
            e.target.tagName === "INPUT" ||
            e.target.tagName === "TEXTAREA" ||
            e.target.tagName === "SELECT"))
      ) {
        return;
      }

      if (e.code === "Space") {
        e.preventDefault();
        handleToggleFlip();
      } else if (e.code === "ArrowLeft") {
        e.preventDefault();
        playFlip();
        setIsFlipped(false);
      } else if (e.code === "ArrowRight") {
        e.preventDefault();
        playFlip();
        setIsFlipped(true);
      } else if (e.code === "KeyR") {
        e.preventDefault();
        handlePronounce();
      } else if (isFlipped) {
        if (e.key === "1") void handleSm2Review("again");
        if (e.key === "2") void handleSm2Review("hard");
        if (e.key === "3") void handleSm2Review("good");
        if (e.key === "4") void handleSm2Review("easy");
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isFlipped, isCompleted, handlePronounce, handleSm2Review, handleToggleFlip, playFlip]);

  const handleRestart = () => {
    setCurrentIndex(0);
    setIsFlipped(false);
    setIsCompleted(false);
    setReviewedCardsCount(0);
    setXpGainedSession(0);
    setStartTime(Date.now());
    setReviewError(null);
  };

  if (isCompleted) {
    return (
      <section className="study-complete" aria-live="polite">
        <span className="study-complete-mark">
          <BookOpen aria-hidden="true" />
        </span>
        <p className="study-eyebrow">{t("Hoàn thành phiên học", "Session complete")}</p>
        <h2>{t("Tốt lắm. Hãy duy trì nhịp học.", "Great work. Keep your learning rhythm.")}</h2>
        <p className="study-complete-copy">
          {t(
            `Bạn đã ôn ${reviewedCardsCount} từ trong phiên này.`,
            `You reviewed ${reviewedCardsCount} words in this session.`
          )}
        </p>
        <div className="study-complete-xp">
          <Zap aria-hidden="true" /> +{xpGainedSession} XP
        </div>
        <div className="study-complete-actions">
          <button type="button" onClick={handleRestart} className="study-restart">
            {t("Ôn lại bộ thẻ", "Review again")}
          </button>
          <Link href="/" className="study-home">
            {t("Về tổng quan", "Back to overview")} <ArrowRight aria-hidden="true" />
          </Link>
        </div>
      </section>
    );
  }

  if (isLoadingVocabulary && !vocabularyList) {
    return (
      <div className="study-empty" role="status">
        {t("Đang tải từ vựng từ cơ sở dữ liệu…", "Loading vocabulary from the database…")}
      </div>
    );
  }

  if (vocabularyLoadError && !vocabularyList) {
    return (
      <div className="study-empty" role="alert">
        <BookOpen aria-hidden="true" />
        <h2>{t("Không tải được bộ thẻ", "Could not load cards")}</h2>
        <p>{vocabularyLoadError}</p>
        <button
          type="button"
          className="study-restart"
          onClick={() => setReloadNonce((n) => n + 1)}
        >
          <RefreshCw size={14} aria-hidden="true" /> {t("Thử tải lại", "Try again")}
        </button>
      </div>
    );
  }

  if (!cards.length) {
    return (
      <div className="study-empty" role="status">
        <BookOpen aria-hidden="true" />
        <h2>{t("Chưa có thẻ để ôn", "No cards to review")}</h2>
        <p>{t("Danh sách từ vựng hiện đang trống.", "The vocabulary list is empty.")}</p>
      </div>
    );
  }

  const renderExample = (sentence: string) => {
    const escapedTerm = currentVocab.term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const parts = sentence.split(new RegExp(`(${escapedTerm})`, "gi"));
    return parts.map((part, index) =>
      part.toLowerCase() === currentVocab.term.toLowerCase() ? (
        <strong key={index} className="study-example-word">
          {part}
        </strong>
      ) : (
        <React.Fragment key={index}>{part}</React.Fragment>
      )
    );
  };

  return (
    <section className="study-cockpit" aria-label={t("Phiên ôn từ vựng", "Vocabulary review session")}>
      {/* Progress bar integrated tightly at top of cockpit when rendered standalone */}
      {!onProgressChange && (
        <div className="study-cockpit-progress">
          <div className="study-progress-row">
            <div className="study-progress-copy">
              <span>{t("Tiến độ phiên học", "Session progress")}</span>
              <strong>
                {currentIndex + 1}
                <span> / {cards.length}</span>
              </strong>
            </div>
            <span className="study-progress-percent">
              {progressPercent}% · +{xpGainedSession} XP
            </span>
          </div>
          <div
            className="study-progress-track"
            role="progressbar"
            aria-label={t("Tiến độ phiên học", "Session progress")}
            aria-valuemin={0}
            aria-valuemax={cards.length}
            aria-valuenow={currentIndex + 1}
          >
            <span style={{ width: `${progressPercent}%` }} />
          </div>
        </div>
      )}

      <div className="study-split-grid">
        {/* LEFT COLUMN (62%): 3D Flashcard */}
        <div className="study-card-col" data-tour="learn-card">
          <div
            className={`study-card-stage ${isFlipped ? "is-flipped" : ""}`}
            onClick={(event) => {
              if (!(event.target as HTMLElement).closest("button")) handleToggleFlip();
            }}
          >
            <div className="study-card" aria-live="polite">
              <article
                className="study-card-face study-card-front"
                aria-hidden={isFlipped}
                inert={isFlipped}
              >
                <div className="study-card-meta">
                  <div className="study-tags">
                    {currentVocab.cefr_level && (
                      <span className="study-level">{currentVocab.cefr_level}</span>
                    )}
                    <span className="study-pos">
                      {currentVocab.part_of_speech || t("Từ vựng", "Vocabulary")}
                    </span>
                  </div>
                  <span className="study-card-index">
                    {String(currentIndex + 1).padStart(2, "0")}{" "}
                    <span>/ {String(cards.length).padStart(2, "0")}</span>
                  </span>
                </div>
                <div className="study-word-block">
                  <span className="study-eyebrow">{t("Từ vựng tiếng Anh", "English vocabulary")}</span>
                  <h1>{currentVocab.term}</h1>
                  {currentVocab.ipa && <p className="study-ipa">{currentVocab.ipa}</p>}
                  <button
                    type="button"
                    onClick={handlePronounce}
                    className={`study-audio ${isSpeaking ? "is-speaking" : ""}`}
                    aria-label={`${t("Nghe phát âm", "Play pronunciation")} ${currentVocab.term}`}
                  >
                    <Volume2 aria-hidden="true" />{" "}
                    <span>{isSpeaking ? t("Đang phát", "Playing") : t("Nghe phát âm", "Play pronunciation")}</span>
                  </button>
                  {speechUnavailable && (
                    <p className="study-audio-note">
                      {t("Trình duyệt không hỗ trợ phát âm.", "This browser does not support pronunciation.")}
                    </p>
                  )}
                </div>
                <button
                  type="button"
                  className="study-flip-prompt"
                  onClick={handleToggleFlip}
                  aria-label={t("Lật thẻ để xem nghĩa", "Flip card to reveal meaning")}
                >
                  <span>{t("Chạm thẻ hoặc nhấn Space để xem nghĩa", "Tap card or press Space to reveal meaning")}</span>
                  <RotateCw aria-hidden="true" />
                </button>
              </article>

              <article
                className="study-card-face study-card-back"
                aria-hidden={!isFlipped}
                inert={!isFlipped}
              >
                <div className="study-card-meta">
                  <span className="study-pos">{t("Nghĩa và ví dụ", "Meaning and example")}</span>
                  <button
                    type="button"
                    onClick={handlePronounce}
                    className="study-audio study-audio-small"
                    aria-label={`${t("Nghe phát âm", "Play pronunciation")} ${currentVocab.term}`}
                  >
                    <Volume2 aria-hidden="true" />
                  </button>
                </div>
                <div className="study-answer">
                  <span className="study-eyebrow">{t("Nghĩa tiếng Việt", "Vietnamese meaning")}</span>
                  <p className="study-meaning">{currentVocab.meaning}</p>
                  {currentVocab.examples?.[0] && (
                    <div className="study-example">
                      <span className="study-eyebrow">{t("Trong ngữ cảnh", "In context")}</span>
                      <p lang="en">&ldquo;{renderExample(currentVocab.examples[0].source_text)}&rdquo;</p>
                      {currentVocab.examples[0].target_text && (
                        <p className="study-translation">{currentVocab.examples[0].target_text}</p>
                      )}
                    </div>
                  )}
                </div>
                <button type="button" className="study-flip-prompt" onClick={handleToggleFlip}>
                  <span>{t("Quay lại mặt trước", "Back to front")}</span>
                  <RotateCw aria-hidden="true" />
                </button>
              </article>
            </div>
          </div>
        </div>

        {/* RIGHT COLUMN (38%): Controls, SM-2 2x2 Grid & Shortcuts (Zero-Scroll Desktop) */}
        <aside className="study-controls-panel" aria-label={t("Bảng điều khiển ôn thẻ", "Flashcard controls")}>
          <div className="study-panel-actions" data-tour="learn-controls">
            <button
              type="button"
              onClick={handleToggleFlip}
              className="study-primary-flip"
              aria-expanded={isFlipped}
            >
              <RotateCw size={16} aria-hidden="true" />
              <span>{isFlipped ? t("Xem mặt trước", "View front") : t("Lật thẻ · Hiện đáp án", "Flip · Show answer")}</span>
              <kbd>Space</kbd>
            </button>

            <div className="study-secondary-row">
              <button
                type="button"
                onClick={handlePronounce}
                className={`study-panel-util ${isSpeaking ? "is-speaking" : ""}`}
              >
                <Volume2 size={15} aria-hidden="true" />
                <span>{t("Nghe phát âm", "Pronounce")}</span>
                <kbd>R</kbd>
              </button>
              <button
                type="button"
                onClick={toggleSound}
                className={`study-panel-sfx ${sfxEnabled ? "is-on" : ""}`}
                aria-pressed={sfxEnabled}
                aria-label={sfxEnabled ? t("Tắt hiệu ứng âm thanh", "Mute sound effects") : t("Bật hiệu ứng âm thanh", "Enable sound effects")}
                title={sfxEnabled ? t("Tắt âm thanh SFX", "Mute SFX") : t("Bật âm thanh SFX", "Enable SFX")}
              >
                {sfxEnabled ? <Volume2 size={15} /> : <VolumeX size={15} />}
                <span>{sfxEnabled ? t("SFX Bật", "SFX On") : t("SFX Tắt", "SFX Off")}</span>
              </button>
            </div>
          </div>

          <div className={`study-review study-review-panel ${isFlipped ? "is-ready" : ""}`} data-tour="learn-ratings" aria-label={t("Đánh giá mức độ ghi nhớ", "Rate your memory")}>
            {reviewError && (
              <p className="study-audio-note" role="alert">
                {reviewError}
              </p>
            )}
            <div className="study-review-heading">
              <span>
                {isFlipped
                  ? t("Bạn nhớ từ này thế nào?", "How well do you remember?")
                  : t("Lật thẻ để mở đánh giá SM-2", "Flip card to rate with SM-2")}
              </span>
              {recentXpPopup ? (
                <span className="study-xp-pop">+{recentXpPopup} XP</span>
              ) : (
                <span className="study-shortcuts">
                  <Keyboard aria-hidden="true" /> {t("Phím 1–4", "Keys 1–4")}
                </span>
              )}
            </div>

            <div className="study-rating-grid study-rating-grid-2x2">
              {([
                ["again", t("Lại", "Again"), t("Quên", "Forgot"), buttonIntervals.again],
                ["hard", t("Khó", "Hard"), t("Chưa chắc", "Difficult"), buttonIntervals.hard],
                ["good", t("Tốt", "Good"), t("Nhớ đúng", "Good"), buttonIntervals.good],
                ["easy", t("Rất dễ", "Easy"), t("Thuộc lòng", "Instant"), buttonIntervals.easy],
              ] as const).map(([rating, label, hint, interval], index) => (
                <button
                  key={rating}
                  type="button"
                  disabled={!isFlipped || isSubmitting}
                  onClick={() => void handleSm2Review(rating)}
                  className={`study-rating study-rating-${rating}`}
                >
                  <div className="study-rating-top">
                    <span className="study-rating-interval">{interval}</span>
                    <kbd>{index + 1}</kbd>
                  </div>
                  <strong>{label}</strong>
                  <span className="study-rating-hint">{hint}</span>
                </button>
              ))}
            </div>
          </div>

          <div className="study-shortcut-legend" aria-label={t("Phím tắt", "Keyboard shortcuts")}>
            <span>
              <kbd>Space</kbd> {t("Lật thẻ", "Flip")}
            </span>
            <span>
              <kbd>R</kbd> {t("Phát âm", "Audio")}
            </span>
            <span>
              <kbd>1</kbd>–<kbd>4</kbd> {t("Đánh giá SM-2", "SM-2 rate")}
            </span>
          </div>
        </aside>
      </div>
    </section>
  );
}
