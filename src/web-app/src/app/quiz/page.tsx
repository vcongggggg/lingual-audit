"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import {
  HelpCircle,
  Clock,
  Zap,
  Flame,
  XCircle,
  ArrowRight,
  RotateCw,
  ChevronLeft,
  Sparkles,
  BookOpen,
  Check,
  X,
  Keyboard,
} from "lucide-react";

// ============================================================================
// 1. DATA TYPES & INTERFACES
// ============================================================================

export interface QuizOption {
  key: "A" | "B" | "C" | "D";
  text: string;
  is_correct: boolean;
}

export interface QuizQuestion {
  id: string;
  sort_order: number;
  level: "A1" | "A2" | "B1" | "B2";
  difficulty: number;
  question_type: string;
  prompt: string;
  options: QuizOption[];
  correct_answer: "A" | "B" | "C" | "D";
  explanation: string;
  points: number;
}

// Sample initial 20 questions loaded from the 300-question bank
const INITIAL_QUIZ_SAMPLE: QuizQuestion[] = [
  {
    id: "quiz-01",
    sort_order: 1,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "She showed remarkable ________ by reviewing 30 flashcards every morning without missing a single day.",
    options: [
      { key: "A", text: "perseverance", is_correct: true },
      { key: "B", text: "hesitation", is_correct: false },
      { key: "C", text: "reluctance", is_correct: false },
      { key: "D", text: "negligence", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (perseverance - sự kiên trì). Ngữ cảnh 'reviewing flashcards every morning without missing a single day' (học mỗi sáng không bỏ sót ngày nào) thể hiện tính bền chí, kiên trì.",
    points: 10,
  },
  {
    id: "quiz-02",
    sort_order: 2,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "In order to win the Clan War, all members need to ________ closely and share vocabulary strategies.",
    options: [
      { key: "A", text: "compete", is_correct: false },
      { key: "B", text: "collaborate", is_correct: true },
      { key: "C", text: "hesitate", is_correct: false },
      { key: "D", text: "isolate", is_correct: false },
    ],
    correct_answer: "B",
    explanation: "Đáp án đúng là B (collaborate - hợp tác, cộng tác). 'Collaborate closely' nghĩa là làm việc và phối hợp chặt chẽ với nhau để đạt mục tiêu chung của bang hội.",
    points: 10,
  },
  {
    id: "quiz-03",
    sort_order: 3,
    level: "B2",
    difficulty: 3,
    question_type: "multiple_choice",
    prompt: "The team leader was ________ in checking every translation error, ensuring the deck was 100% accurate.",
    options: [
      { key: "A", text: "meticulous", is_correct: true },
      { key: "B", text: "careless", is_correct: false },
      { key: "C", text: "superficial", is_correct: false },
      { key: "D", text: "indifferent", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (meticulous - tỉ mỉ, kỹ lưỡng). Việc 'kiểm tra từng lỗi dịch thuật để đạt 100% chính xác' thể hiện sự cẩn thận đến từng chi tiết nhỏ.",
    points: 10,
  },
  {
    id: "quiz-04",
    sort_order: 4,
    level: "A2",
    difficulty: 1,
    question_type: "multiple_choice",
    prompt: "Learning a new foreign language is a wonderful way to broaden your ________.",
    options: [
      { key: "A", text: "horizons", is_correct: true },
      { key: "B", text: "boundaries", is_correct: false },
      { key: "C", text: "limitations", is_correct: false },
      { key: "D", text: "barriers", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (horizons). Thành ngữ cố định phổ biến: 'broaden one's horizons' (mở rộng tầm mắt, hiểu biết và thế giới quan).",
    points: 10,
  },
  {
    id: "quiz-05",
    sort_order: 5,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "Don't ________ until tomorrow what you can review today, or you will risk losing your 14-day streak!",
    options: [
      { key: "A", text: "put off", is_correct: true },
      { key: "B", text: "put on", is_correct: false },
      { key: "C", text: "put out", is_correct: false },
      { key: "D", text: "put up", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (put off - trì hoãn, hoãn lại = postpone). Tục ngữ: 'Đừng để việc hôm nay đến ngày mai mới làm'.",
    points: 10,
  },
  {
    id: "quiz-06",
    sort_order: 6,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "Consistent daily practice plays a ________ role in retaining vocabulary in long-term memory.",
    options: [
      { key: "A", text: "crucial", is_correct: true },
      { key: "B", text: "trivial", is_correct: false },
      { key: "C", text: "minor", is_correct: false },
      { key: "D", text: "negligible", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (crucial - cốt lõi, có tính quyết định). Collocation chuẩn: 'play a crucial role in sth'.",
    points: 10,
  },
  {
    id: "quiz-07",
    sort_order: 7,
    level: "A2",
    difficulty: 1,
    question_type: "multiple_choice",
    prompt: "She speaks English so ________ that native speakers often mistake her for an American resident.",
    options: [
      { key: "A", text: "fluently", is_correct: true },
      { key: "B", text: "harshly", is_correct: false },
      { key: "C", text: "clumsily", is_correct: false },
      { key: "D", text: "vaguely", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (fluently - trôi chảy, lưu loát). Bổ nghĩa cho động từ 'speaks English'.",
    points: 10,
  },
  {
    id: "quiz-08",
    sort_order: 8,
    level: "B2",
    difficulty: 3,
    question_type: "multiple_choice",
    prompt: "Applying the Spaced Repetition (SRS SM-2) algorithm was a major ________ that boosted users' retention rate by 40%.",
    options: [
      { key: "A", text: "breakdown", is_correct: false },
      { key: "B", "text": "breakthrough", is_correct: true },
      { key: "C", text: "setback", is_correct: false },
      { key: "D", text: "drawback", is_correct: false },
    ],
    correct_answer: "B",
    explanation: "Đáp án đúng là B (breakthrough - bước đột phá). 'Boosted retention rate by 40%' chỉ một thành tựu tích cực vượt bậc.",
    points: 10,
  },
  {
    id: "quiz-09",
    sort_order: 9,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "If you want to reach Level 20 this season, you must ________ a steady study habit every evening.",
    options: [
      { key: "A", text: "maintain", is_correct: true },
      { key: "B", text: "abandon", is_correct: false },
      { key: "C", text: "interrupt", is_correct: false },
      { key: "D", text: "neglect", is_correct: false },
    ],
    correct_answer: "A",
    explanation: "Đáp án đúng là A (maintain - duy trì, giữ vững thói quen). Collocation: 'maintain a habit'.",
    points: 10,
  },
  {
    id: "quiz-10",
    sort_order: 10,
    level: "B1",
    difficulty: 2,
    question_type: "multiple_choice",
    prompt: "Junior developers always ________ senior mentors for their exceptional expertise and patient guidance.",
    options: [
      { key: "A", text: "look down on", is_correct: false },
      { key: "B", text: "look up to", is_correct: true },
      { key: "C", text: "look out for", is_correct: false },
      { key: "D", text: "look into", is_correct: false },
    ],
    correct_answer: "B",
    explanation: "Đáp án đúng là B (look up to - ngưỡng mộ, kính trọng = admire).",
    points: 10,
  },
];

// ============================================================================
// 2. MAIN QUIZ PRACTICE COMPONENT
// ============================================================================

export default function QuizPracticePage() {
  // Session Configuration States
  const questions = INITIAL_QUIZ_SAMPLE;
  const [currentIndex, setCurrentIndex] = useState(0);
  const [selectedKey, setSelectedKey] = useState<"A" | "B" | "C" | "D" | null>(null);
  const [isAnswered, setIsAnswered] = useState(false);
  const [timerSeconds, setTimerSeconds] = useState(30);
  const [score, setScore] = useState(0);
  const [totalXp, setTotalXp] = useState(0);
  const [comboStreak, setComboStreak] = useState(0);
  const [isCompleted, setIsCompleted] = useState(false);
  const [wrongAnswersList, setWrongAnswersList] = useState<{ question: QuizQuestion; yourChoice: string }[]>([]);
  const [selectedMode, setSelectedMode] = useState<"sprint10" | "clan20" | "marathon">("sprint10");

  const currentQuestion = questions[currentIndex] || questions[0];

  const handleSelectAnswer = useCallback((key: "A" | "B" | "C" | "D", isTimeout = false) => {
    if (isAnswered) return;
    setSelectedKey(key);
    setIsAnswered(true);

    const isCorrect = !isTimeout && key === currentQuestion.correct_answer;
    if (isCorrect) {
      const xpEarned = 10 + Math.min(15, comboStreak * 3);
      setScore((prev) => prev + 1);
      setTotalXp((prev) => prev + xpEarned);
      setComboStreak((prev) => prev + 1);
    } else {
      setComboStreak(0);
      setWrongAnswersList((prev) => [
        ...prev,
        { question: currentQuestion, yourChoice: isTimeout ? "H\u1ebft gi\u1edd" : key },
      ]);
    }
  }, [comboStreak, currentQuestion, isAnswered]);

  const handleNextQuestion = useCallback(() => {
    if (currentIndex + 1 < questions.length) {
      setCurrentIndex((prev) => prev + 1);
      setSelectedKey(null);
      setIsAnswered(false);
      setTimerSeconds(30);
    } else {
      setIsCompleted(true);
    }
  }, [currentIndex, questions.length]);

  useEffect(() => {
    if (isAnswered || isCompleted) return;
    if (timerSeconds <= 0) {
      const timeout = setTimeout(() => handleSelectAnswer("A", true), 0);
      return () => clearTimeout(timeout);
    }
    const timer = setInterval(() => setTimerSeconds((prev) => prev - 1), 1000);
    return () => clearInterval(timer);
  }, [timerSeconds, isAnswered, isCompleted, handleSelectAnswer]);

  // Restart Quiz
  const handleRestart = () => {
    setCurrentIndex(0);
    setSelectedKey(null);
    setIsAnswered(false);
    setTimerSeconds(30);
    setScore(0);
    setTotalXp(0);
    setComboStreak(0);
    setIsCompleted(false);
    setWrongAnswersList([]);
  };

  // Switch Quiz Mode
  const handleSwitchMode = (mode: "sprint10" | "clan20" | "marathon") => {
    setSelectedMode(mode);
    handleRestart();
  };

  // Keyboard Shortcuts (1-4 or A-D, Space/Enter to next)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (isCompleted) return;

      if (!isAnswered) {
        if (e.key === "1" || e.key.toUpperCase() === "A") handleSelectAnswer("A");
        if (e.key === "2" || e.key.toUpperCase() === "B") handleSelectAnswer("B");
        if (e.key === "3" || e.key.toUpperCase() === "C") handleSelectAnswer("C");
        if (e.key === "4" || e.key.toUpperCase() === "D") handleSelectAnswer("D");
      } else {
        if (e.code === "Space" || e.code === "Enter") {
          e.preventDefault();
          handleNextQuestion();
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isAnswered, isCompleted, handleNextQuestion, handleSelectAnswer]);

  // ============================================================================
  // RENDER: RESULTS SCREEN (KẾT THÚC BÀI THI)
  // ============================================================================
  if (isCompleted) {
    const percentage = Math.round((score / questions.length) * 100);

    return (
      <div className="quiz-page min-h-screen bg-[#070b14] text-slate-100 flex flex-col justify-between p-4 md:py-8 relative overflow-x-hidden">
        {/* Background ambient glow */}
        <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
          <div className="absolute -top-40 left-1/2 -translate-x-1/2 w-[600px] h-[300px] bg-indigo-600/20 blur-[130px] rounded-full" />
        </div>

        <div className="w-full max-w-xl mx-auto flex flex-col items-center text-center z-10">
          {/* Trophy Avatar */}
          <div className="w-20 h-20 rounded-3xl bg-gradient-to-tr from-amber-400 via-yellow-400 to-amber-600 flex items-center justify-center text-4xl shadow-2xl shadow-amber-500/30 mb-3 animate-bounce">
            🏆
          </div>

          <h1 className="text-2xl sm:text-3xl font-black text-white">Hoàn Thành Bài Ôn Tập!</h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Bạn đã nỗ lực ôn tập kho trắc nghiệm từ vựng chuẩn bị cho Clan War.
          </p>

          {/* Scorecards */}
          <div className="grid grid-cols-3 gap-2.5 w-full my-6">
            <div className="p-3.5 rounded-2xl bg-slate-900/80 border border-white/5 flex flex-col items-center">
              <span className="text-[10px] text-slate-400 uppercase font-bold">Số câu đúng</span>
              <span className="text-2xl font-black text-white mt-0.5">
                {score} <span className="text-xs font-semibold text-slate-500">/ {questions.length}</span>
              </span>
              <span className="text-[10px] text-emerald-400 font-semibold mt-0.5">{percentage}% Chính xác</span>
            </div>

            <div className="p-3.5 rounded-2xl bg-slate-900/80 border border-white/5 flex flex-col items-center">
              <span className="text-[10px] text-slate-400 uppercase font-bold">XP Tích Lũy</span>
              <span className="text-2xl font-black text-amber-400 mt-0.5 flex items-center gap-1">
                <Zap className="w-5 h-5 fill-amber-400" />
                +{totalXp}
              </span>
              <span className="text-[10px] text-indigo-300 font-semibold mt-0.5">Cộng vào Clan</span>
            </div>

            <div className="p-3.5 rounded-2xl bg-slate-900/80 border border-white/5 flex flex-col items-center">
              <span className="text-[10px] text-slate-400 uppercase font-bold">Cấp bậc</span>
              <span className="text-2xl font-black text-cyan-400 mt-0.5">
                {percentage >= 80 ? "Sử Thi" : percentage >= 50 ? "Khá Tốt" : "Cần Ôn"}
              </span>
              <span className="text-[10px] text-slate-400 font-semibold mt-0.5">Level A2+</span>
            </div>
          </div>

          {/* Wrong answers review section if any */}
          {wrongAnswersList.length > 0 && (
            <div className="w-full bg-slate-900/70 rounded-2xl p-4 border border-rose-500/20 text-left my-2 mb-6">
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-bold text-rose-400 flex items-center gap-1.5">
                  <XCircle className="w-4 h-4" />
                  Các câu cần xem lại ({wrongAnswersList.length} câu)
                </span>
                <span className="text-[10px] text-slate-400">Đã tự động đưa vào hàng đợi SRS</span>
              </div>

              <div className="flex flex-col gap-2 max-h-56 overflow-y-auto pr-1">
                {wrongAnswersList.map((item, idx) => (
                  <div key={idx} className="p-2.5 rounded-xl bg-slate-950/60 border border-white/5 text-xs">
                    <p className="font-semibold text-slate-200">{item.question.prompt}</p>
                    <div className="flex items-center gap-3 mt-1.5 text-[11px]">
                      <span className="text-rose-400">Bạn chọn: <strong>{item.yourChoice}</strong></span>
                      <span className="text-emerald-400">Đáp án đúng: <strong>{item.question.correct_answer}</strong></span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Action CTAs */}
          <div className="flex flex-col sm:flex-row gap-3 w-full">
            <button
              onClick={handleRestart}
              className="flex-1 py-3 px-4 rounded-2xl bg-slate-800 hover:bg-slate-700 text-white font-bold text-sm flex items-center justify-center gap-2 transition-all border border-white/10"
            >
              <RotateCw className="w-4 h-4 text-indigo-400" />
              <span>Làm Lại Bộ Này</span>
            </button>
            <Link
              href="/learn"
              className="flex-1 py-3 px-4 rounded-2xl bg-slate-800 hover:bg-slate-700 text-white font-bold text-sm flex items-center justify-center gap-2 transition-all border border-white/10"
            >
              <BookOpen className="w-4 h-4 text-cyan-400" />
              <span>Học Flashcard 3D</span>
            </Link>
            <Link
              href="/"
              className="flex-1 py-3 px-4 rounded-2xl bg-gradient-to-r from-indigo-600 to-cyan-500 hover:from-indigo-500 hover:to-cyan-400 text-white font-black text-sm flex items-center justify-center gap-2 shadow-lg shadow-indigo-600/30 transition-all"
            >
              <span>Xem BXH</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // ============================================================================
  // RENDER: ACTIVE QUIZ PRACTICE QUESTION SCREEN
  // ============================================================================
  const progressPercent = Math.round(((currentIndex + 1) / questions.length) * 100);

  return (
    <div className="quiz-page min-h-screen bg-[#070b14] text-slate-100 flex flex-col justify-between p-4 md:py-6 relative overflow-x-hidden select-none">
      {/* Background ambient lighting */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
        <div className="absolute -top-40 left-1/2 -translate-x-1/2 w-[600px] h-[300px] bg-indigo-600/20 blur-[130px] rounded-full" />
        <div className="absolute top-[40%] -left-32 w-80 h-80 bg-cyan-500/10 blur-[100px] rounded-full" />
        <div className="absolute top-[60%] -right-32 w-80 h-80 bg-amber-500/10 blur-[100px] rounded-full" />
      </div>

      {/* Header Navigation */}
      <header className="w-full max-w-xl mx-auto flex items-center justify-between pb-3 border-b border-white/5 z-10">
        <Link
          href="/"
          className="flex items-center gap-1.5 text-xs font-bold text-slate-300 hover:text-white bg-slate-900/80 hover:bg-slate-800 px-3 py-1.5 rounded-xl border border-white/10 transition-colors"
        >
          <ChevronLeft className="w-4 h-4" />
          <span>Bảng Xếp Hạng</span>
        </Link>

        {/* Mode Selector Tabs */}
        <div className="flex p-0.5 bg-slate-900/90 rounded-xl border border-white/10 text-xs font-semibold">
          <button
            onClick={() => handleSwitchMode("sprint10")}
            className={`px-2.5 py-1 rounded-lg transition-colors ${
              selectedMode === "sprint10" ? "bg-indigo-600 text-white shadow" : "text-slate-400 hover:text-white"
            }`}
          >
            Sprint 10 câu
          </button>
          <button
            onClick={() => handleSwitchMode("clan20")}
            className={`px-2.5 py-1 rounded-lg transition-colors ${
              selectedMode === "clan20" ? "bg-indigo-600 text-white shadow" : "text-slate-400 hover:text-white"
            }`}
          >
            Clan War 20 câu
          </button>
          <button
            onClick={() => handleSwitchMode("marathon")}
            className={`px-2.5 py-1 rounded-lg transition-colors ${
              selectedMode === "marathon" ? "bg-indigo-600 text-white shadow" : "text-slate-400 hover:text-white"
            }`}
          >
            Kho 300 câu
          </button>
        </div>

        <Link
          href="/learn"
          className="flex items-center gap-1 text-xs font-bold text-cyan-400 bg-cyan-400/10 px-2.5 py-1.5 rounded-xl border border-cyan-400/20"
        >
          <BookOpen className="w-3.5 h-3.5" />
          <span className="hidden sm:inline">Flashcard</span>
        </Link>
      </header>

      {/* Main Study Container */}
      <main className="w-full flex-1 flex flex-col items-center justify-center py-4 z-10">
        <div className="w-full max-w-xl mx-auto flex flex-col">
          {/* Top Progress Bar & Live Session Stats */}
          <div className="w-full flex items-center justify-between mb-3 text-xs px-1">
            <div className="flex items-center gap-2">
              <span className="font-extrabold text-white text-sm">
                Câu {currentIndex + 1}{" "}
                <span className="text-slate-500 text-xs">/ {questions.length}</span>
              </span>
              <div className="w-24 sm:w-36 h-2 bg-slate-800 rounded-full overflow-hidden border border-white/5">
                <div
                  style={{ width: `${progressPercent}%` }}
                  className="h-full bg-gradient-to-r from-indigo-500 via-indigo-400 to-cyan-400 rounded-full transition-all duration-300"
                />
              </div>
            </div>

            {/* Countdown Timer & Combo streak */}
            <div className="flex items-center gap-2.5">
              {comboStreak > 1 && (
                <div className="flex items-center gap-1 font-bold text-amber-400 bg-amber-400/10 px-2 py-0.5 rounded-xl border border-amber-400/20 animate-pulse">
                  <Flame className="w-3.5 h-3.5 fill-amber-400" />
                  <span>Combo x{comboStreak}!</span>
                </div>
              )}

              <div
                className={`flex items-center gap-1 font-bold px-2.5 py-0.5 rounded-xl border ${
                  timerSeconds <= 5
                    ? "text-rose-400 bg-rose-500/10 border-rose-500/30 animate-ping"
                    : "text-slate-300 bg-slate-900/80 border-white/10"
                }`}
              >
                <Clock className="w-3.5 h-3.5 text-slate-400" />
                <span>{timerSeconds}s</span>
              </div>

              <div className="flex items-center gap-1 font-black text-cyan-400 bg-cyan-400/10 px-2.5 py-0.5 rounded-xl border border-cyan-400/20">
                <Zap className="w-3.5 h-3.5 fill-cyan-400" />
                <span>{totalXp} XP</span>
              </div>
            </div>
          </div>

          {/* QUESTION CARD */}
          <div className="w-full rounded-3xl bg-gradient-to-b from-indigo-950/40 via-slate-900/80 to-slate-950/90 border border-white/10 p-5 sm:p-6 shadow-2xl backdrop-blur-xl relative overflow-hidden">
            {/* Metadata Tags */}
            <div className="flex items-center justify-between mb-3 text-xs">
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-black uppercase tracking-wider text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded-md border border-indigo-500/20">
                  Cấp độ {currentQuestion.level}
                </span>
                <span className="text-[10px] text-slate-400 font-semibold bg-slate-800/80 px-2 py-0.5 rounded-md border border-white/5">
                  Độ khó {currentQuestion.difficulty}/3
                </span>
              </div>
              <span className="text-amber-400 font-bold flex items-center gap-1 text-[11px]">
                <Sparkles className="w-3.5 h-3.5" />
                +{currentQuestion.points} Điểm
              </span>
            </div>

            {/* Question Prompt */}
            <h2 className="text-lg sm:text-xl font-bold text-white leading-relaxed my-2">
              {currentQuestion.prompt}
            </h2>

            {/* 4 CHOICES (A, B, C, D) */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 mt-5">
              {currentQuestion.options.map((option) => {
                const isSelected = selectedKey === option.key;
                const isCorrect = option.key === currentQuestion.correct_answer;

                let btnStyle = "bg-slate-900/80 hover:bg-slate-800/80 border-white/10 text-slate-200";

                if (isAnswered) {
                  if (isCorrect) {
                    btnStyle = "bg-emerald-950/60 border-emerald-400 text-emerald-200 ring-2 ring-emerald-400/40 font-black";
                  } else if (isSelected && !isCorrect) {
                    btnStyle = "bg-rose-950/60 border-rose-500 text-rose-200 ring-1 ring-rose-500/40 font-bold";
                  } else {
                    btnStyle = "bg-slate-950/40 border-white/5 text-slate-500 opacity-60";
                  }
                }

                return (
                  <button
                    key={option.key}
                    disabled={isAnswered}
                    onClick={() => handleSelectAnswer(option.key)}
                    className={`flex items-center justify-between p-3.5 rounded-2xl border text-sm font-semibold transition-all duration-200 active:scale-98 ${btnStyle}`}
                  >
                    <div className="flex items-center gap-3">
                      <span
                        className={`w-7 h-7 rounded-xl flex items-center justify-center font-black text-xs ${
                          isAnswered && isCorrect
                            ? "bg-emerald-500 text-slate-950"
                            : isAnswered && isSelected && !isCorrect
                            ? "bg-rose-500 text-white"
                            : "bg-slate-800 text-indigo-300 border border-white/10"
                        }`}
                      >
                        {option.key}
                      </span>
                      <span>{option.text}</span>
                    </div>

                    {/* Result icon */}
                    {isAnswered && isCorrect && <Check className="w-5 h-5 text-emerald-400" />}
                    {isAnswered && isSelected && !isCorrect && <X className="w-5 h-5 text-rose-400" />}
                  </button>
                );
              })}
            </div>

            {/* EXPLANATION BOX (HIỆN NGAY KHI TRẢ LỜI XONG) */}
            {isAnswered && (
              <div className="mt-4 p-4 rounded-2xl bg-gradient-to-r from-indigo-950/70 via-slate-900 to-slate-900 border border-indigo-500/30 text-xs text-left animate-fadeIn">
                <div className="flex items-center gap-2 mb-1 text-indigo-300 font-extrabold uppercase tracking-wider text-[11px]">
                  <HelpCircle className="w-4 h-4 text-cyan-400" />
                  <span>Giải Thích Chi Tiết:</span>
                </div>
                <p className="text-slate-200 leading-relaxed font-medium">
                  {currentQuestion.explanation}
                </p>
              </div>
            )}
          </div>

          {/* BOTTOM CONTROLS & NEXT BUTTON */}
          <div className="flex items-center justify-between mt-4 px-1">
            <span className="text-[11px] text-slate-500 flex items-center gap-1">
              <Keyboard className="w-3.5 h-3.5 text-slate-500" />
              Phím tắt: 1-4 hoặc A-D để chọn • Phím Space/Enter câu tiếp theo
            </span>

            {isAnswered && (
              <button
                onClick={handleNextQuestion}
                className="py-2.5 px-6 rounded-2xl bg-gradient-to-r from-indigo-600 to-cyan-500 hover:from-indigo-500 hover:to-cyan-400 text-white font-black text-xs md:text-sm shadow-lg shadow-indigo-600/30 transition-transform active:scale-95 flex items-center gap-2 animate-bounce"
              >
                <span>Câu Tiếp Theo</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="w-full max-w-xl mx-auto pt-3 border-t border-white/5 flex items-center justify-between text-[11px] text-slate-500 z-10">
        <span>Ngân hàng 300 câu hỏi trắc nghiệm CEFR A1-B2</span>
        <span>LinguaMezon • Quiz Clan</span>
      </footer>
    </div>
  );
}

