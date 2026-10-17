"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import Link from "next/link";
import {
  Volume2,
  RotateCw,
  Zap,
  ArrowRight,
  BookOpen,
  Keyboard,
} from "lucide-react";

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
  cefr_level?: "A1" | "A2";
  part_of_speech?: string; // noun, verb, adjective, adverb...
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
  quality: number; // 0-5
  repetitions: number;
  interval_days: number;
  ease_factor: number;
  next_due_at: Date;
  interval_label: string;
}

// Payload sent to ASP.NET Core 8 Backend: POST /api/v1/srs/review
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
// 2. SM-2 ALGORITHM CLIENT HELPER (SUPERMEMO-2 STANDARD)
// ============================================================================

/**
 * Calculates new interval, ease factor, and repetition according to SM-2 formula:
 * EF' = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
 * Min EF = 1.30
 */
export function calculateSm2(
  currentState: { repetitions: number; interval_days: number; ease_factor: number },
  rating: Sm2Rating
): Sm2CalculationResult {
  const qualityMap: Record<Sm2Rating, number> = {
    again: 1, // Complete blackout / reset
    hard: 2,  // Hard recall / minor penalty
    good: 4,  // Good recall / standard growth
    easy: 5,  // Instant recall / bonus growth
  };

  const q = qualityMap[rating];
  let { repetitions, interval_days } = currentState;
  const ease_factor = currentState.ease_factor;

  if (q < 3) {
    // Again / Fail: Reset repetitions back to 0, review again today (10 minutes or next day)
    repetitions = 0;
    interval_days = 0.007; // ~10 minutes in days
  } else {
    // Success: Calculate new intervals
    if (repetitions === 0) {
      interval_days = 1; // 1 day
    } else if (repetitions === 1) {
      interval_days = rating === "hard" ? 3 : 6; // 3 to 6 days
    } else {
      let multiplier = ease_factor;
      if (rating === "hard") multiplier = 1.2;
      if (rating === "easy") multiplier = ease_factor * 1.3;
      interval_days = Math.round(interval_days * multiplier * 10) / 10;
    }
    repetitions += 1;
  }

  // Update Ease Factor (EF)
  let newEaseFactor = ease_factor + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02));
  if (newEaseFactor < 1.3) newEaseFactor = 1.3; // SM-2 standard lower bound
  newEaseFactor = Math.round(newEaseFactor * 100) / 100;

  // Next Due Date
  const nextDueDate = new Date(Date.now() + interval_days * 24 * 60 * 60 * 1000);

  // User-friendly label for button badge (e.g. <10m, 1d, 3d, 6d)
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
// 3. MOCK VOCABULARY DATA (A1-A2 CURRICULUM ON MEZON)
// ============================================================================

const MOCK_VOCABULARY_LIST: VocabularyItem[] = [
  {
    id: "vocab-01",
    term: "Perseverance",
    normalized_term: "perseverance",
    source_language: "en",
    target_language: "vi",
    ipa: "/ˌpɜː.sɪˈvɪə.rəns/",
    part_of_speech: "noun",
    meaning: "Tính kiên trì, sự bền chí, quyết tâm theo đuổi mục tiêu dù gặp muôn vàn khó khăn.",
    image_url: "https://images.unsplash.com/photo-1519834785169-98be25ec3f84?w=600&auto=format&fit=crop&q=80",
    examples: [
      {
        source_text: "Through hard work and perseverance, she finally conquered the IELTS 8.0.",
        target_text: "Nhờ làm việc chăm chỉ và sự kiên trì bền bỉ, cô ấy cuối cùng đã chinh phục được IELTS 8.0.",
      },
      {
        source_text: "His perseverance in learning 20 words every day on Mezon inspired the whole clan.",
        target_text: "Sự kiên trì học 20 từ mỗi ngày trên Mezon của anh ấy đã truyền cảm hứng cho cả bang hội.",
      },
    ],
    collocations: ["show perseverance", "remarkable perseverance", "rewarded for perseverance"],
  },
  {
    id: "vocab-02",
    term: "Eloquent",
    normalized_term: "eloquent",
    source_language: "en",
    target_language: "vi",
    ipa: "/ˈel.ə.kwənt/",
    part_of_speech: "adjective",
    meaning: "Hùng biện, lưu loát, có tài ăn nói thuyết phục và truyền cảm hứng.",
    image_url: "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=600&auto=format&fit=crop&q=80",
    examples: [
      {
        source_text: "The student gave an eloquent presentation about AI in language education.",
        target_text: "Bạn sinh viên đã có một bài thuyết trình hùng biện đầy thuyết phục về AI trong giáo dục ngôn ngữ.",
      },
    ],
    collocations: ["eloquent speaker", "eloquent speech", "eloquent testimony"],
  },
  {
    id: "vocab-03",
    term: "Meticulous",
    normalized_term: "meticulous",
    source_language: "en",
    target_language: "vi",
    ipa: "/məˈtɪk.jə.ləs/",
    part_of_speech: "adjective",
    meaning: "Tỉ mỉ, cẩn thận từng li từng tí, chú ý đến từng chi tiết nhỏ nhất.",
    image_url: "https://images.unsplash.com/photo-1455849318743-b2233052fcff?w=600&auto=format&fit=crop&q=80",
    examples: [
      {
        source_text: "He kept meticulous records of every SRS flashcard he reviewed.",
        target_text: "Anh ấy ghi chép tỉ mỉ từng thẻ flashcard SRS mà mình đã ôn tập.",
      },
    ],
    collocations: ["meticulous research", "meticulous attention to detail", "meticulous planning"],
  },
];

// ============================================================================
// 4. MAIN 3D FLASHCARD COMPONENT
// ============================================================================

interface FlashcardStudyProps {
  vocabularyList?: VocabularyItem[];
  onFinishSession?: (stats: { totalReviewed: number; xpEarned: number }) => void;
  onReviewSubmit?: (payload: SrsReviewPayload) => Promise<void> | void;
}

export default function FlashcardStudy({
  vocabularyList = MOCK_VOCABULARY_LIST,
  onFinishSession,
  onReviewSubmit,
}: FlashcardStudyProps) {
  // Navigation & Card Flip State
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [speechUnavailable, setSpeechUnavailable] = useState(false);
  const [xpGainedSession, setXpGainedSession] = useState(0);
  const [recentXpPopup, setRecentXpPopup] = useState<number | null>(null);
  const [reviewedCardsCount, setReviewedCardsCount] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [startTime, setStartTime] = useState<number>(() => Date.now());
  const [isCompleted, setIsCompleted] = useState(false);

  // Mock initial SRS states for the deck
  const [srsDeckState, setSrsDeckState] = useState<Record<string, SrsCardState>>({
    "vocab-01": {
      user_id: "user-current",
      vocabulary_id: "vocab-01",
      repetitions: 1,
      interval_days: 1.0,
      ease_factor: 2.5,
      next_due_at: new Date().toISOString(),
      lapses: 0,
    },
    "vocab-02": {
      user_id: "user-current",
      vocabulary_id: "vocab-02",
      repetitions: 0,
      interval_days: 0,
      ease_factor: 2.5,
      next_due_at: new Date().toISOString(),
      lapses: 0,
    },
    "vocab-03": {
      user_id: "user-current",
      vocabulary_id: "vocab-03",
      repetitions: 3,
      interval_days: 6.0,
      ease_factor: 2.6,
      next_due_at: new Date().toISOString(),
      lapses: 0,
    },
  });

  const currentVocab = vocabularyList[currentIndex] || vocabularyList[0];
  const currentCardSrs = useMemo(() => srsDeckState[currentVocab.id] || {
    user_id: "user-current",
    vocabulary_id: currentVocab.id,
    repetitions: 0,
    interval_days: 0,
    ease_factor: 2.5,
    next_due_at: "",
    lapses: 0,
  }, [currentVocab.id, srsDeckState]);

  // Preview SM-2 interval labels for the 4 buttons in real-time
  const buttonIntervals = {
    again: calculateSm2(currentCardSrs, "again").interval_label,
    hard: calculateSm2(currentCardSrs, "hard").interval_label,
    good: calculateSm2(currentCardSrs, "good").interval_label,
    easy: calculateSm2(currentCardSrs, "easy").interval_label,
  };

  // Web Speech API: Pronounce the English term
  const handlePronounce = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation(); // Prevent card from flipping when clicking audio button
      if (typeof window === "undefined" || !("speechSynthesis" in window)) {
        setSpeechUnavailable(true);
        return;
      }
      setSpeechUnavailable(false);

      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(currentVocab.term);
      utterance.lang = "en-US";
      utterance.rate = 0.88; // Natural language cadence
      utterance.pitch = 1.0;

      utterance.onstart = () => setIsSpeaking(true);
      utterance.onend = () => setIsSpeaking(false);
      utterance.onerror = () => setIsSpeaking(false);

      window.speechSynthesis.speak(utterance);
    },
    [currentVocab.term]
  );

  // Toggle 3D Card Flip
  const handleToggleFlip = useCallback(() => {
    if (isSubmitting) return;
    setIsFlipped((prev) => !prev);
  }, [isSubmitting]);

  // SM-2 Review Button Handler
  const handleSm2Review = useCallback(async (rating: Sm2Rating) => {
    if (isSubmitting) return;
    setIsSubmitting(true);

    const timeSpentMs = Date.now() - startTime;
    const sm2Result = calculateSm2(currentCardSrs, rating);

    // Calculate XP based on rating: Easy = 25XP, Good = 20XP, Hard = 15XP, Again = 5XP
    const xpMap: Record<Sm2Rating, number> = {
      again: 5,
      hard: 15,
      good: 20,
      easy: 25,
    };
    const earnedXp = xpMap[rating];

    // Build Backend Payload
    const payload: SrsReviewPayload = {
      vocabulary_id: currentVocab.id,
      rating,
      quality: sm2Result.quality,
      repetitions: sm2Result.repetitions,
      interval_days: sm2Result.interval_days,
      ease_factor: sm2Result.ease_factor,
      next_due_at: sm2Result.next_due_at.toISOString(),
      time_spent_ms: timeSpentMs,
      idempotency_key: `${currentVocab.id}-${Date.now()}`,
    };

    // Update Local SRS State
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

    // Trigger XP animation
    setRecentXpPopup(earnedXp);
    setXpGainedSession((prev) => prev + earnedXp);
    setReviewedCardsCount((prev) => prev + 1);

    // Invoke external API prop if supplied
    if (onReviewSubmit) {
      try {
        await onReviewSubmit(payload);
      } catch (err) {
        console.error("Lỗi khi gửi kết quả SRS lên server:", err);
      }
    } else {
      // Default log to console for development verification
      console.log("🚀 [SM-2 Review Payload Ready For Backend]:", payload);
    }

    // Smooth transition to next card after 350ms
    setTimeout(() => {
      setRecentXpPopup(null);
      setIsFlipped(false);
      setIsSubmitting(false);
      setStartTime(Date.now());

      if (currentIndex + 1 < vocabularyList.length) {
        setCurrentIndex((prev) => prev + 1);
      } else {
        setIsCompleted(true);
        if (onFinishSession) {
          onFinishSession({
            totalReviewed: reviewedCardsCount + 1,
            xpEarned: xpGainedSession + earnedXp,
          });
        }
      }
    }, 400);
  }, [
    currentCardSrs,
    currentIndex,
    currentVocab.id,
    isSubmitting,
    onFinishSession,
    onReviewSubmit,
    reviewedCardsCount,
    startTime,
    vocabularyList.length,
    xpGainedSession,
  ]);

  // Keyboard Shortcuts (Space to flip, 1-4 for SM-2 rating, R for audio)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (isCompleted) return;
      if (e.repeat || (e.target instanceof HTMLElement && e.target.isContentEditable)) return;

      if (e.code === "Space") {
        e.preventDefault();
        handleToggleFlip();
      } else if (e.code === "KeyR") {
        e.preventDefault();
        handlePronounce();
      } else if (isFlipped) {
        if (e.key === "1") handleSm2Review("again");
        if (e.key === "2") handleSm2Review("hard");
        if (e.key === "3") handleSm2Review("good");
        if (e.key === "4") handleSm2Review("easy");
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isFlipped, isCompleted, handlePronounce, handleSm2Review, handleToggleFlip]);

  // Restart session
  const handleRestart = () => {
    setCurrentIndex(0);
    setIsFlipped(false);
    setIsCompleted(false);
    setReviewedCardsCount(0);
    setXpGainedSession(0);
    setStartTime(Date.now());
  };

  // ============================================================================
  // RENDER: COMPLETED SESSION SUMMARY SCREEN
  // ============================================================================
  if (isCompleted) {
    return (
      <section className="study-complete" aria-live="polite">
        <span className="study-complete-mark"><BookOpen aria-hidden="true" /></span>
        <p className="study-eyebrow">Hoàn thành phiên học</p>
        <h2>Tốt lắm. Hãy duy trì nhịp học.</h2>
        <p className="study-complete-copy">Bạn đã ôn {reviewedCardsCount} từ trong phiên này.</p>
        <div className="study-complete-xp"><Zap aria-hidden="true" /> +{xpGainedSession} XP</div>
        <div className="study-complete-actions">
          <button type="button" onClick={handleRestart} className="study-restart">Ôn lại bộ thẻ</button>
          <Link href="/" className="study-home">Về tổng quan <ArrowRight aria-hidden="true" /></Link>
        </div>
      </section>
    );
  }

  // Active study card
  const progressPercent = vocabularyList.length ? Math.round(((currentIndex + 1) / vocabularyList.length) * 100) : 0;

  if (!vocabularyList.length) {
    return <div className="study-empty" role="status"><BookOpen aria-hidden="true" /><h2>Chưa có thẻ để ôn</h2><p>Danh sách từ vựng hiện đang trống.</p></div>;
  }

  const renderExample = (sentence: string) => {
    const escapedTerm = currentVocab.term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const parts = sentence.split(new RegExp(`(${escapedTerm})`, "gi"));
    return parts.map((part, index) => part.toLowerCase() === currentVocab.term.toLowerCase()
      ? <strong key={index} className="study-example-word">{part}</strong>
      : <React.Fragment key={index}>{part}</React.Fragment>);
  };

  return (
    <section className="study-shell" aria-label="Phiên ôn từ vựng">
      <div className="study-progress-row">
        <div className="study-progress-copy"><span>Tiến độ phiên học</span><strong>{currentIndex + 1}<span> / {vocabularyList.length}</span></strong></div>
        <span className="study-progress-percent">{progressPercent}%</span>
      </div>
      <div className="study-progress-track" role="progressbar" aria-label="Tiến độ phiên học" aria-valuemin={0} aria-valuemax={vocabularyList.length} aria-valuenow={currentIndex + 1}>
        <span style={{ width: `${progressPercent}%` }} />
      </div>

      <div className={`study-card-stage ${isFlipped ? "is-flipped" : ""}`} onClick={(event) => {
        if (!(event.target as HTMLElement).closest("button")) handleToggleFlip();
      }}>
        <div className="study-card" aria-live="polite">
          <article className="study-card-face study-card-front" aria-hidden={isFlipped} inert={isFlipped}>
            <div className="study-card-meta">
              <div className="study-tags">
                {currentVocab.cefr_level && <span className="study-level">{currentVocab.cefr_level}</span>}
                <span className="study-pos">{currentVocab.part_of_speech || "Vocabulary"}</span>
              </div>
              <span className="study-card-index">{String(currentIndex + 1).padStart(2, "0")} <span>/ {String(vocabularyList.length).padStart(2, "0")}</span></span>
            </div>
            <div className="study-word-block">
              <span className="study-eyebrow">Từ vựng tiếng Anh</span>
              <h1>{currentVocab.term}</h1>
              {currentVocab.ipa && <p className="study-ipa">{currentVocab.ipa}</p>}
              <button type="button" onClick={handlePronounce} className={`study-audio ${isSpeaking ? "is-speaking" : ""}`} aria-label={`Play pronunciation for ${currentVocab.term}`}>
                <Volume2 aria-hidden="true" /> <span>{isSpeaking ? "Đang phát" : "Nghe phát âm"}</span>
              </button>
              {speechUnavailable && <p className="study-audio-note">Trình duyệt không hỗ trợ phát âm.</p>}
            </div>
            <button type="button" className="study-flip-prompt" onClick={handleToggleFlip} aria-label="Flip card to reveal meaning">
              <span>Chạm để xem nghĩa</span><RotateCw aria-hidden="true" />
            </button>
          </article>

          <article className="study-card-face study-card-back" aria-hidden={!isFlipped} inert={!isFlipped}>
            <div className="study-card-meta">
              <span className="study-pos">Nghĩa và ví dụ</span>
              <button type="button" onClick={handlePronounce} className="study-audio study-audio-small" aria-label={`Nghe phát âm ${currentVocab.term}`}><Volume2 aria-hidden="true" /></button>
            </div>
            <div className="study-answer">
              <span className="study-eyebrow">Nghĩa tiếng Việt</span>
              <p className="study-meaning">{currentVocab.meaning}</p>
              {currentVocab.examples?.[0] && <div className="study-example"><span className="study-eyebrow">Trong ngữ cảnh</span><p lang="en">&ldquo;{renderExample(currentVocab.examples[0].source_text)}&rdquo;</p>{currentVocab.examples[0].target_text && <p className="study-translation">{currentVocab.examples[0].target_text}</p>}</div>}
            </div>
            <button type="button" className="study-flip-prompt" onClick={handleToggleFlip}><span>Quay lại mặt trước</span><RotateCw aria-hidden="true" /></button>
          </article>
        </div>
      </div>

      <button type="button" onClick={handleToggleFlip} className="study-reveal-link" aria-expanded={isFlipped}>
        <RotateCw aria-hidden="true" /> {isFlipped ? "Xem mặt trước" : "Hiện đáp án"}
      </button>

      <div className={`study-review ${isFlipped ? "is-ready" : ""}`} aria-label="Đánh giá mức độ ghi nhớ">
        <div className="study-review-heading"><span>{isFlipped ? "Bạn nhớ từ này thế nào?" : "Lật thẻ để chọn mức ghi nhớ"}</span>{recentXpPopup ? <span>+{recentXpPopup} XP</span> : <span className="study-shortcuts"><Keyboard aria-hidden="true" /> Phím 1–4</span>}</div>
        <div className="study-rating-grid">
          {([
            ["again", "Again", "Quên", buttonIntervals.again],
            ["hard", "Hard", "Khó", buttonIntervals.hard],
            ["good", "Good", "Tốt", buttonIntervals.good],
            ["easy", "Easy", "Rất dễ", buttonIntervals.easy],
          ] as const).map(([rating, label, hint, interval], index) => (
            <button key={rating} type="button" disabled={!isFlipped || isSubmitting} onClick={() => handleSm2Review(rating)} className={`study-rating study-rating-${rating}`}>
              <span className="study-rating-interval">{interval}</span><strong>{label}</strong><span className="study-rating-hint">{hint} <kbd>{index + 1}</kbd></span>
            </button>
          ))}
        </div>
      </div>
    </section>
  );
}
