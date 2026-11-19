"use client";

import React, { useState, useEffect, useRef, useCallback } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Gamepad2,
  Trophy,
  Clock,
  Sparkles,
  Zap,
  Volume2,
  X,
} from "lucide-react";
import {
  getGameData,
  submitGameResult,
  submitWordleGuess,
  getGameLeaderboard,
  getMyGameStats,
  type GameSubmitResult,
  type GameLeaderboardEntry,
  type MyGameStatsDto,
} from "@/lib/api";

import { GamePersonalStats } from "./components/GamePersonalStats";
import { GameCatalog, type GameCatalogItem } from "./components/GameCatalog";
import { GameLeaderboardTab } from "./components/GameLeaderboardTab";
import { GameResultModal } from "./components/GameResultModal";
import { WordMatchGame } from "./components/WordMatchGame";
import { SentenceScrambleGame } from "./components/SentenceScrambleGame";
import { TypingRaceGame } from "./components/TypingRaceGame";
import { FillBlitzGame } from "./components/FillBlitzGame";
import { SoundReflexGame } from "./components/SoundReflexGame";
import { LingoWordleGame } from "./components/LingoWordleGame";

interface ScrambleItem {
  id: string;
  prompt: string;
  tokens: string[];
  level: string;
}

interface TypingItem {
  id: string;
  targetText: string;
  phonetic: string;
  translation: string;
  cefrLevel: string;
}

interface FillItem {
  id: string;
  prompt: string;
  choices: string[];
  level: string;
}

interface SoundItem {
  id: string;
  audioText: string;
  choices: { id: string; targetText: string; translation: string }[];
}

export default function GamesHubPage() {
  // Navigation tab: "catalog" or "leaderboard"
  const [activeTab, setActiveTab] = useState<"catalog" | "leaderboard">("catalog");
  const [selectedDifficulty, setSelectedDifficulty] = useState<string>("all");

  // Active game session
  const [activeGame, setActiveGame] = useState<string | null>(null);
  const [attemptId, setAttemptId] = useState<string>("");
  const [gameLoading, setGameLoading] = useState<boolean>(false);
  const [submitting, setSubmitting] = useState<boolean>(false);

  // Live game metrics
  const [timeRemaining, setTimeRemaining] = useState<number>(60);
  const [currentScore, setCurrentScore] = useState<number>(0);
  const [combo, setCombo] = useState<number>(0);

  // Personal stats & leaderboard
  const [stats, setStats] = useState<MyGameStatsDto | null>(null);
  const [leaderboard, setLeaderboard] = useState<GameLeaderboardEntry[]>([]);
  const [leaderboardLoading, setLeaderboardLoading] = useState<boolean>(false);
  const [leaderboardFilter, setLeaderboardFilter] = useState<string>("all");

  // Summary modal
  const [gameSummary, setGameSummary] = useState<GameSubmitResult | null>(null);

  // 1. Word Match state
  const [matchLeft, setMatchLeft] = useState<{ id: string; text: string }[]>([]);
  const [matchRight, setMatchRight] = useState<{ id: string; text: string }[]>([]);
  const [selectedLeft, setSelectedLeft] = useState<string | null>(null);
  const [matchedPairs, setMatchedPairs] = useState<Set<string>>(new Set());
  const [wrongMatch, setWrongMatch] = useState<string | null>(null);

  // 2. Sentence Scramble state
  const [scrambleQuestions, setScrambleQuestions] = useState<ScrambleItem[]>([]);
  const [scrambleIndex, setScrambleIndex] = useState<number>(0);
  const [placedTokens, setPlacedTokens] = useState<string[]>([]);
  const [availableTokens, setAvailableTokens] = useState<string[]>([]);

  // 3. Typing Race state
  const [typingWords, setTypingWords] = useState<TypingItem[]>([]);
  const [typingIndex, setTypingIndex] = useState<number>(0);
  const [typingInput, setTypingInput] = useState<string>("");
  const [typingSuccess, setTypingSuccess] = useState<boolean>(false);

  // 4. Fill Blitz state
  const [fillQuestions, setFillQuestions] = useState<FillItem[]>([]);
  const [fillIndex, setFillIndex] = useState<number>(0);

  // 5. Sound Reflex state
  const [soundQuestions, setSoundQuestions] = useState<SoundItem[]>([]);
  const [soundIndex, setSoundIndex] = useState<number>(0);

  // 6. Lingo Wordle state
  const [wordlePrompt, setWordlePrompt] = useState<string>("");
  const [wordleCategory, setWordleCategory] = useState<string>("");
  const [wordleMeaningVi, setWordleMeaningVi] = useState<string>("");
  const [wordleGuesses, setWordleGuesses] = useState<string[]>([]);
  const [wordleEvaluations, setWordleEvaluations] = useState<string[][]>([]);
  const [currentGuess, setCurrentGuess] = useState<string>("");
  const [wordleKeyColors, setWordleKeyColors] = useState<Record<string, string>>({});
  const [wordleGameOver, setWordleGameOver] = useState<boolean>(false);

  // Buffer to accumulate user answers for server anti-cheat submission
  const answersBuffer = useRef<{ questionId: string; answer: string }[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  // Audio synthesizer via Web Audio API
  const playSynthesizedSound = (type: "coin" | "victory" | "buzzer") => {
    try {
      if (typeof window === "undefined") return;
      const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = new AudioCtx();

      if (type === "coin") {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = "sine";
        osc.frequency.setValueAtTime(987.77, ctx.currentTime);
        osc.frequency.setValueAtTime(1318.51, ctx.currentTime + 0.08);
        gain.gain.setValueAtTime(0.15, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        osc.stop(ctx.currentTime + 0.35);
      } else if (type === "victory") {
        const notes = [523.25, 659.25, 783.99, 1046.5];
        notes.forEach((freq, idx) => {
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          osc.type = "triangle";
          osc.frequency.setValueAtTime(freq, ctx.currentTime + idx * 0.1);
          gain.gain.setValueAtTime(0.15, ctx.currentTime + idx * 0.1);
          gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + idx * 0.1 + 0.25);
          osc.connect(gain);
          gain.connect(ctx.destination);
          osc.start(ctx.currentTime + idx * 0.1);
          osc.stop(ctx.currentTime + idx * 0.1 + 0.25);
        });
      } else if (type === "buzzer") {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = "sawtooth";
        osc.frequency.setValueAtTime(150, ctx.currentTime);
        gain.gain.setValueAtTime(0.12, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        osc.stop(ctx.currentTime + 0.25);
      }
    } catch {
      // AudioCtx not allowed without user interaction
    }
  };

  const speakWord = (text: string) => {
    try {
      if (typeof window !== "undefined" && "speechSynthesis" in window) {
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-US";
        utterance.rate = 0.85;
        window.speechSynthesis.speak(utterance);
      }
    } catch {
      // SpeechSynthesis unavailable
    }
  };

  // Fetch personal stats
  const refreshStats = useCallback(async () => {
    try {
      const res = await getMyGameStats();
      if (res && res.success) {
        setStats(res);
      }
    } catch {
      // Not authenticated or stats empty
    }
  }, []);

  // Fetch leaderboard
  const refreshLeaderboard = useCallback(async (filter?: string) => {
    setLeaderboardLoading(true);
    try {
      const res = await getGameLeaderboard(filter === "all" ? undefined : filter, 20);
      if (res && res.entries) {
        setLeaderboard(res.entries);
      }
    } catch {
      setLeaderboard([]);
    } finally {
      setLeaderboardLoading(false);
    }
  }, []);

  // Submit and Finish Game
  const handleFinishGame = useCallback(async (timeLeft: number) => {
    if (submitting || !attemptId || !activeGame) return;
    setSubmitting(true);

    try {
      const payload = {
        attemptId,
        gameType: activeGame,
        timeRemainingSeconds: Math.max(0, timeLeft),
        answers: answersBuffer.current,
      };

      const result = await submitGameResult(payload);
      if (result && result.success) {
        setGameSummary(result);
        playSynthesizedSound("victory");
        refreshStats();
        refreshLeaderboard(leaderboardFilter);
      } else {
        alert("Không thể lưu kết quả trò chơi.");
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Lỗi khi nộp kết quả trò chơi.";
      alert(msg);
    } finally {
      setSubmitting(false);
    }
  }, [submitting, attemptId, activeGame, refreshStats, refreshLeaderboard, leaderboardFilter]);

  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        const statsRes = await getMyGameStats();
        if (isMounted && statsRes && statsRes.success) {
          setStats(statsRes);
        }
      } catch {}
      try {
        const lbRes = await getGameLeaderboard(undefined, 20);
        if (isMounted && lbRes && lbRes.entries) {
          setLeaderboard(lbRes.entries);
        }
      } catch {}
    })();

    return () => {
      isMounted = false;
    };
  }, []);

  const handleLeaderboardFilterChange = (type: string) => {
    setLeaderboardFilter(type);
    refreshLeaderboard(type);
  };

  // Timer countdown hook
  useEffect(() => {
    if (!activeGame || activeGame === "lingo_wordle" || gameSummary) {
      if (timerRef.current) clearInterval(timerRef.current);
      return;
    }

    timerRef.current = setInterval(() => {
      setTimeRemaining((prev) => {
        if (prev <= 1) {
          clearInterval(timerRef.current!);
          handleFinishGame(0);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [activeGame, gameSummary, handleFinishGame]);

  // Start game handler
  const handleStartGame = async (type: string) => {
    setGameLoading(true);
    setGameSummary(null);
    setCurrentScore(0);
    setCombo(0);
    setTimeRemaining(60);
    answersBuffer.current = [];

    try {
      const res = await getGameData(type, selectedDifficulty);
      if (!res || !res.data) {
        alert("Không thể tải dữ liệu trò chơi. Vui lòng thử lại!");
        setGameLoading(false);
        return;
      }

      setAttemptId(res.data.attemptId || "");
      setActiveGame(type);

      // Initialize game modes
      if (type === "word_match" && res.data.items) {
        const items = res.data.items;
        setMatchLeft(items.map((i) => ({ id: i.id, text: i.targetText })));
        setMatchRight(
          items
            .map((i) => ({ id: i.id, text: i.translation }))
            .sort(() => Math.random() - 0.5)
        );
        setSelectedLeft(null);
        setMatchedPairs(new Set());
      } else if (type === "sentence_scramble" && res.data.questions) {
        const questions = res.data.questions as unknown as ScrambleItem[];
        setScrambleQuestions(questions);
        setScrambleIndex(0);
        if (questions[0]) {
          setAvailableTokens([...questions[0].tokens].sort(() => Math.random() - 0.5));
          setPlacedTokens([]);
        }
      } else if (type === "typing_race" && res.data.words) {
        const words = res.data.words as unknown as TypingItem[];
        setTypingWords(words);
        setTypingIndex(0);
        setTypingInput("");
        setTypingSuccess(false);
      } else if (type === "fill_blitz" && res.data.questions) {
        const questions = res.data.questions as unknown as FillItem[];
        setFillQuestions(questions);
        setFillIndex(0);
      } else if (type === "sound_reflex" && res.data.questions) {
        const questions = res.data.questions as unknown as SoundItem[];
        setSoundQuestions(questions);
        setSoundIndex(0);
        if (questions[0]?.audioText) {
          setTimeout(() => speakWord(questions[0].audioText), 400);
        }
      } else if (type === "lingo_wordle" && res.data) {
        setWordlePrompt(res.data.prompt || "");
        setWordleCategory(res.data.category || "");
        setWordleMeaningVi(res.data.meaningVi || "");
        setWordleGuesses([]);
        setWordleEvaluations([]);
        setCurrentGuess("");
        setWordleKeyColors({});
        setWordleGameOver(false);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Lỗi khi kết nối máy chủ trò chơi.";
      alert(msg);
    } finally {
      setGameLoading(false);
    }
  };

  // 1. Word Match Handlers
  const handleSelectLeft = (id: string) => {
    if (matchedPairs.has(id)) return;
    setSelectedLeft(id);
  };

  const handleSelectRight = (id: string) => {
    if (matchedPairs.has(id) || !selectedLeft) return;

    answersBuffer.current.push({
      questionId: selectedLeft,
      answer: matchRight.find((r) => r.id === id)?.text || "",
    });

    if (selectedLeft === id) {
      playSynthesizedSound("coin");
      const nextMatched = new Set(matchedPairs);
      nextMatched.add(id);
      setMatchedPairs(nextMatched);
      setSelectedLeft(null);
      setCurrentScore((prev) => prev + 25);
      setCombo((prev) => prev + 1);

      if (nextMatched.size === matchLeft.length) {
        setTimeout(() => handleFinishGame(timeRemaining), 600);
      }
    } else {
      playSynthesizedSound("buzzer");
      setWrongMatch(id);
      setCombo(0);
      setTimeout(() => {
        setWrongMatch(null);
        setSelectedLeft(null);
      }, 350);
    }
  };

  // 2. Sentence Scramble Handlers
  const handlePlaceToken = (token: string, index: number) => {
    setPlacedTokens((prev) => [...prev, token]);
    setAvailableTokens((prev) => prev.filter((_, i) => i !== index));
  };

  const handleRemoveToken = (token: string, index: number) => {
    setAvailableTokens((prev) => [...prev, token]);
    setPlacedTokens((prev) => prev.filter((_, i) => i !== index));
  };

  const handleConfirmScramble = () => {
    const q = scrambleQuestions[scrambleIndex];
    if (!q) return;

    const sentence = placedTokens.join(" ");
    answersBuffer.current.push({
      questionId: q.id,
      answer: sentence,
    });

    playSynthesizedSound("coin");
    setCurrentScore((prev) => prev + 30);
    setCombo((prev) => prev + 1);

    const nextIndex = scrambleIndex + 1;
    if (nextIndex < scrambleQuestions.length) {
      setScrambleIndex(nextIndex);
      setPlacedTokens([]);
      setAvailableTokens([...scrambleQuestions[nextIndex].tokens].sort(() => Math.random() - 0.5));
    } else {
      handleFinishGame(timeRemaining);
    }
  };

  // 3. Typing Race Handlers
  const handleTypingChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setTypingInput(val);

    const currentWord = typingWords[typingIndex];
    if (!currentWord) return;

    if (val.trim().toLowerCase() === currentWord.targetText.trim().toLowerCase()) {
      playSynthesizedSound("coin");
      setTypingSuccess(true);
      setCurrentScore((prev) => prev + 40);
      setCombo((prev) => prev + 1);

      answersBuffer.current.push({
        questionId: currentWord.id,
        answer: currentWord.targetText,
      });

      setTimeout(() => {
        setTypingSuccess(false);
        setTypingInput("");
        const nextIdx = typingIndex + 1;
        if (nextIdx < typingWords.length) {
          setTypingIndex(nextIdx);
        } else {
          handleFinishGame(timeRemaining);
        }
      }, 700);
    }
  };

  // 4. Fill Blitz Handlers
  const handleSelectFillChoice = (choice: string) => {
    const q = fillQuestions[fillIndex];
    if (!q) return;

    answersBuffer.current.push({
      questionId: q.id,
      answer: choice,
    });

    playSynthesizedSound("coin");
    setCurrentScore((prev) => prev + 20);
    setCombo((prev) => prev + 1);

    const nextIdx = fillIndex + 1;
    if (nextIdx < fillQuestions.length) {
      setFillIndex(nextIdx);
    } else {
      handleFinishGame(timeRemaining);
    }
  };

  // 5. Sound Reflex Handlers
  const handleSelectSoundChoice = (choiceText: string) => {
    const q = soundQuestions[soundIndex];
    if (!q) return;

    answersBuffer.current.push({
      questionId: q.id,
      answer: choiceText,
    });

    playSynthesizedSound("coin");
    setCurrentScore((prev) => prev + 35);
    setCombo((prev) => prev + 1);

    const nextIdx = soundIndex + 1;
    if (nextIdx < soundQuestions.length) {
      setSoundIndex(nextIdx);
      if (soundQuestions[nextIdx]?.audioText) {
        setTimeout(() => speakWord(soundQuestions[nextIdx].audioText), 400);
      }
    } else {
      handleFinishGame(timeRemaining);
    }
  };

  // 6. Lingo Wordle Handlers
  const handleWordleKeyPress = async (key: string) => {
    if (wordleGameOver) return;

    if (key === "ENTER") {
      if (currentGuess.length !== 5) {
        alert("Từ đoán phải gồm đúng 5 chữ cái!");
        return;
      }

      const guessWord = currentGuess.toUpperCase();
      try {
        const res = await submitWordleGuess(attemptId, guessWord);
        if (!res.success) {
          alert(res.message || "Không thể kiểm tra từ đoán.");
          return;
        }

        answersBuffer.current.push({
          questionId: attemptId,
          answer: guessWord,
        });

        const nextGuesses = [...wordleGuesses, guessWord];
        setWordleGuesses(nextGuesses);
        setWordleEvaluations((prev) => [...prev, res.evaluations]);
        setCurrentGuess("");

        // Update keyboard colors according to highest priority: is-correct > is-present > is-absent
        setWordleKeyColors((prev) => {
          const updated = { ...prev };
          const priority: Record<string, number> = {
            "is-correct": 3,
            "is-present": 2,
            "is-absent": 1,
          };

          for (let i = 0; i < 5; i++) {
            const letter = guessWord[i];
            const evalState = `is-${res.evaluations[i]}`;
            const currentPriority = priority[updated[letter]] || 0;
            const newPriority = priority[evalState] || 0;
            if (newPriority > currentPriority) {
              updated[letter] = evalState;
            }
          }
          return updated;
        });

        if (res.isWon) {
          playSynthesizedSound("victory");
          setWordleGameOver(true);
          setTimeout(() => handleFinishGame(0), 1000);
        } else if (res.isGameOver || nextGuesses.length >= 6) {
          playSynthesizedSound("buzzer");
          setWordleGameOver(true);
          setTimeout(() => handleFinishGame(0), 1000);
        }
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : "Lỗi khi kiểm tra từ đoán.";
        alert(msg);
      }
    } else if (key === "BACKSPACE" || key === "DEL") {
      setCurrentGuess((prev) => prev.slice(0, -1));
    } else if (/^[A-Za-z]$/.test(key) && currentGuess.length < 5) {
      setCurrentGuess((prev) => prev + key.toUpperCase());
    }
  };

  // Catalog items definition
  const GAME_CATALOG: GameCatalogItem[] = [
    {
      id: "word_match",
      name: "Nối từ nhanh",
      tagline: "Ghép đôi từ vựng tiếng Anh và nghĩa tiếng Việt",
      description: "Thử thách trí nhớ ghép đúng 8 cặp từ vựng. Tích lũy chuỗi combo để nhân bội điểm số.",
      difficulty: "A1",
      timeLimit: 60,
      rewardBase: "+25 điểm / cặp đúng",
      icon: <Zap size={24} />,
    },
    {
      id: "sentence_scramble",
      name: "Xếp câu chuẩn",
      tagline: "Sắp xếp các mảnh từ thành câu hoàn chỉnh",
      description: "Luyện ngữ pháp và cấu trúc câu tự nhiên. Bấm chọn các chip từ theo trật tự chính xác.",
      difficulty: "A2",
      timeLimit: 60,
      rewardBase: "+30 điểm / câu đúng",
      icon: <Sparkles size={24} />,
    },
    {
      id: "typing_race",
      name: "Đua tốc độ gõ",
      tagline: "Luyện gõ từ vựng tiếng Anh và nhớ mặt chữ",
      description: "Quan sát từ vựng, phiên âm IPA và nghĩa hiển thị, gõ lại thật nhanh để sang từ tiếp theo.",
      difficulty: "B1",
      timeLimit: 60,
      rewardBase: "+40 điểm / từ đúng",
      icon: <Clock size={24} />,
    },
    {
      id: "fill_blitz",
      name: "Điền chỗ trống Blitz",
      tagline: "Trắc nghiệm chọn đáp án đúng trong tích tắc",
      description: "Đọc câu hỏi ngữ cảnh và chọn 1 trong 4 phương án chính xác nhất để hoàn thiện câu văn.",
      difficulty: "A2",
      timeLimit: 60,
      rewardBase: "+20 điểm / câu đúng",
      icon: <Trophy size={24} />,
    },
    {
      id: "sound_reflex",
      name: "Phản xạ nghe âm",
      tagline: "Luyện tai bắt âm chuẩn bản xứ",
      description: "Lắng nghe máy phát âm từ vựng và bấm ngay vào nghĩa tiếng Việt tương ứng trong 4 phương án.",
      difficulty: "B1",
      timeLimit: 60,
      rewardBase: "+35 điểm / câu đúng",
      icon: <Volume2 size={24} />,
    },
    {
      id: "lingo_wordle",
      name: "Lingo Wordle",
      tagline: "Đoán từ 5 chữ cái bí ẩn",
      description: "Thử tài suy luận từ vựng 5 ký tự với tối đa 6 lượt đoán. Không áp lực thời gian.",
      difficulty: "B2",
      timeLimit: 0,
      rewardBase: "+50 điểm khi đoán đúng",
      icon: <Gamepad2 size={24} />,
    },
  ];

  return (
    <main className="games-page">
      <div className="games-container">
        {/* Top bar */}
        <div className="games-topbar">
          <Link href="/" className="games-back-link">
            <ArrowLeft size={18} />
            <span>Trang chủ</span>
          </Link>
          <div className="games-nav-tabs">
            <button
              onClick={() => {
                setActiveGame(null);
                setActiveTab("catalog");
              }}
              className={`games-tab-btn ${activeTab === "catalog" && !activeGame ? "is-active" : ""}`}
            >
              <Gamepad2 size={16} />
              <span>Kho trò chơi</span>
            </button>
            <button
              onClick={() => {
                setActiveGame(null);
                setActiveTab("leaderboard");
              }}
              className={`games-tab-btn ${activeTab === "leaderboard" && !activeGame ? "is-active" : ""}`}
            >
              <Trophy size={16} />
              <span>Bảng xếp hạng</span>
            </button>
          </div>
        </div>

        {/* If a game is active: Game Arena Header */}
        {activeGame ? (
          <div className="active-game-header">
            <div className="active-game-info">
              <span className="active-game-badge">
                {GAME_CATALOG.find((g) => g.id === activeGame)?.name || activeGame}
              </span>
              <div className="active-game-stats">
                <span className="live-score">Điểm: {currentScore}</span>
                {combo > 1 && <span className="live-combo">🔥 Combo x{combo}</span>}
              </div>
            </div>

            <div className="active-game-controls">
              {activeGame !== "lingo_wordle" && (
                <div className={`timer-badge ${timeRemaining <= 10 ? "is-urgent" : ""}`}>
                  <Clock size={16} />
                  <span>{timeRemaining}s</span>
                </div>
              )}
              <button
                onClick={() => {
                  if (confirm("Bạn có chắc muốn thoát trò chơi? Tiến trình hiện tại sẽ bị hủy.")) {
                    setActiveGame(null);
                  }
                }}
                className="exit-game-btn"
                title="Thoát trò chơi"
              >
                <X size={18} />
                <span>Thoát</span>
              </button>
            </div>
          </div>
        ) : (
          <div className="games-hero-section">
            <div className="hero-content">
              <span className="hero-eyebrow">Mezon Gaming Arena</span>
              <h1 className="hero-title">Đấu trường trò chơi từ vựng</h1>
              <p className="hero-subtitle">
                Rèn luyện từ vựng, phản xạ nghe và ngữ pháp tiếng Anh thông qua 6 mini-game tương tác.
                Tích lũy điểm số, thăng hạng Leaderboard và gom thưởng XP mỗi ngày!
              </p>
            </div>
            <GamePersonalStats stats={stats} />
          </div>
        )}

        {/* Main Content Area */}
        {activeGame ? (
          <div className="gameplay-arena">
            {activeGame === "word_match" && (
              <WordMatchGame
                matchLeft={matchLeft}
                matchRight={matchRight}
                selectedLeft={selectedLeft}
                wrongMatch={wrongMatch}
                matchedPairs={matchedPairs}
                onSelectLeft={handleSelectLeft}
                onSelectRight={handleSelectRight}
              />
            )}

            {activeGame === "sentence_scramble" && (
              <SentenceScrambleGame
                currentQuestion={scrambleQuestions[scrambleIndex]}
                currentIndex={scrambleIndex}
                totalQuestions={scrambleQuestions.length}
                placedTokens={placedTokens}
                availableTokens={availableTokens}
                onPlaceToken={handlePlaceToken}
                onRemoveToken={handleRemoveToken}
                onConfirmScramble={handleConfirmScramble}
              />
            )}

            {activeGame === "typing_race" && (
              <TypingRaceGame
                currentWord={typingWords[typingIndex]}
                currentIndex={typingIndex}
                totalWords={typingWords.length}
                typingInput={typingInput}
                typingSuccess={typingSuccess}
                onTypingChange={handleTypingChange}
              />
            )}

            {activeGame === "fill_blitz" && (
              <FillBlitzGame
                currentQuestion={fillQuestions[fillIndex]}
                currentIndex={fillIndex}
                totalQuestions={fillQuestions.length}
                onSelectChoice={handleSelectFillChoice}
              />
            )}

            {activeGame === "sound_reflex" && (
              <SoundReflexGame
                currentQuestion={soundQuestions[soundIndex]}
                currentIndex={soundIndex}
                totalQuestions={soundQuestions.length}
                onSelectChoice={handleSelectSoundChoice}
                onReplayAudio={speakWord}
              />
            )}

            {activeGame === "lingo_wordle" && (
              <LingoWordleGame
                prompt={wordlePrompt}
                category={wordleCategory}
                meaningVi={wordleMeaningVi}
                guesses={wordleGuesses}
                evaluations={wordleEvaluations}
                currentGuess={currentGuess}
                keyColors={wordleKeyColors}
                gameOver={wordleGameOver}
                onKeyPress={handleWordleKeyPress}
              />
            )}
          </div>
        ) : activeTab === "catalog" ? (
          <GameCatalog
            catalog={GAME_CATALOG}
            selectedDifficulty={selectedDifficulty}
            onDifficultyChange={setSelectedDifficulty}
            onStartGame={handleStartGame}
            gameLoading={gameLoading}
            stats={stats}
          />
        ) : (
          <GameLeaderboardTab
            leaderboard={leaderboard}
            loading={leaderboardLoading}
            filterType={leaderboardFilter}
            onFilterChange={handleLeaderboardFilterChange}
            onPlayGame={() => setActiveTab("catalog")}
          />
        )}

        {/* Victory / Summary Modal */}
        <GameResultModal
          summary={gameSummary}
          onClose={() => {
            setGameSummary(null);
            setActiveGame(null);
          }}
          onPlayAgain={() => {
            const lastGame = activeGame;
            setGameSummary(null);
            if (lastGame) {
              handleStartGame(lastGame);
            }
          }}
        />
      </div>
    </main>
  );
}
