'use client';

import React, { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import Link from 'next/link';
import {
  Gamepad2,
  Trophy,
  Zap,
  Clock,
  RotateCcw,
  ArrowRight,
  ArrowLeft,
  Volume2,
  CheckCircle,
  Shuffle,
  Keyboard,
  Sparkles,
  Check,
  X,
  Flame,
  BookOpen,
  HelpCircle,
  CheckCircle2,
} from 'lucide-react';
import { useAuth } from '@/components/auth/useAuth';
import {
  getGameData,
  submitGameResult,
  getGameLeaderboard,
  getMyGameStats,
  submitWordleGuess,
  type GameDataPayload,
  type GameLeaderboardEntry,
} from '@/lib/api';

// ==========================================
// 1. GAME CATALOG METADATA
// ==========================================

interface GameCatalogItem {
  id: string;
  apiKey: string;
  title: string;
  englishTitle: string;
  desc: string;
  level: string;
  timeLimit: string;
  xpReward: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
}

const CATALOG_ITEMS: GameCatalogItem[] = [
  {
    id: 'word-match',
    apiKey: 'word_match',
    title: 'Nối từ siêu tốc',
    englishTitle: 'Speed Word Match',
    desc: 'Ghép đôi từ vựng tiếng Anh với nghĩa tiếng Việt chuẩn xác trong thời gian 60 giây. Dữ liệu nạp từ Database 500 từ CEFR.',
    level: 'A1 - B1',
    timeLimit: '60s',
    xpReward: '+25 XP',
    icon: Zap,
  },
  {
    id: 'scramble',
    apiKey: 'sentence_scramble',
    title: 'Xếp câu chuẩn',
    englishTitle: 'Sentence Scramble',
    desc: 'Sắp xếp các mảnh từ bị xáo trộn thành câu văn hoàn chỉnh đúng ngữ pháp và cấu trúc ngữ nghĩa.',
    level: 'A1 - B2',
    timeLimit: '60s',
    xpReward: '+30 XP',
    icon: Shuffle,
  },
  {
    id: 'typing-race',
    apiKey: 'typing_race',
    title: 'Đua tốc độ gõ',
    englishTitle: 'Typing Speedster',
    desc: 'Quan sát từ vựng, phiên âm IPA và nghĩa hiển thị từ Database, gõ lại thật nhanh chính xác để sang từ tiếp theo.',
    level: 'A1 - B2',
    timeLimit: '60s',
    xpReward: '+40 XP',
    icon: Keyboard,
  },
  {
    id: 'word-fill',
    apiKey: 'fill_blitz',
    title: 'Điền khuyết ngữ cảnh',
    englishTitle: 'Context Cloze Blitz',
    desc: 'Đọc câu hỏi ngữ cảnh từ kho 300 câu hỏi và chọn 1 trong 4 phương án chính xác nhất để hoàn thiện câu.',
    level: 'A2 - B2',
    timeLimit: '60s',
    xpReward: '+20 XP',
    icon: CheckCircle,
  },
  {
    id: 'audio-reflex',
    apiKey: 'sound_reflex',
    title: 'Phản xạ nghe âm',
    englishTitle: 'Audio Ear Reflex',
    desc: 'Lắng nghe máy phát âm từ vựng bản xứ và bấm ngay vào nghĩa tiếng Việt tương ứng trong 4 phương án.',
    level: 'A1 - B1',
    timeLimit: '60s',
    xpReward: '+35 XP',
    icon: Volume2,
  },
  {
    id: 'word-guess',
    apiKey: 'lingo_wordle',
    title: 'Lingo Wordle',
    englishTitle: '5-Letter Word Guess',
    desc: 'Thử tài suy luận từ vựng 5 ký tự ngẫu nhiên từ Database với tối đa 6 lượt đoán và phản hồi màu sắc trực quan.',
    level: 'B1 - B2',
    timeLimit: 'Không giới hạn',
    xpReward: '+50 XP',
    icon: Gamepad2,
  },
];

// ==========================================
// 2. MAIN COMPONENT
// ==========================================

export default function GameCenterPage() {
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const [activeTab, setActiveTab] = useState<'catalog' | 'leaderboard'>('catalog');
  const [selectedLevel, setSelectedLevel] = useState<string>('all');
  const [activeGame, setActiveGame] = useState<GameCatalogItem | null>(null);

  // Player Stats (Real stats only, initialized to 0)
  const [stats, setStats] = useState({
    gamesPlayed: 0,
    gamesWon: 0,
    totalXp: 0,
    rankTitle: 'Tập sự',
  });

  useEffect(() => {
    if (!isAuthenticated) return;
    getMyGameStats()
      .then((res) => {
        if (res && res.success) {
          setStats({
            gamesPlayed: res.totalGamesPlayed,
            gamesWon: res.totalWins,
            totalXp: res.totalXpEarned,
            rankTitle: res.totalGamesPlayed > 10 ? 'Đại cao thủ' : res.totalGamesPlayed > 3 ? 'Cao thủ' : 'Tập sự',
          });
        }
      })
      .catch(() => {});
  }, [isAuthenticated, user?.id]);

  const recordGameCompletion = useCallback((won: boolean, xpEarned: number) => {
    setStats((prev) => ({
      ...prev,
      gamesPlayed: prev.gamesPlayed + 1,
      gamesWon: prev.gamesWon + (won ? 1 : 0),
      totalXp: prev.totalXp + xpEarned,
    }));
  }, []);

  // Filter games by CEFR level
  const filteredCatalog = useMemo(() => {
    if (selectedLevel === 'all') return CATALOG_ITEMS;
    return CATALOG_ITEMS.filter((item) => item.level.includes(selectedLevel));
  }, [selectedLevel]);

  return (
    <main className="games-page">
      {/* 1. Header with Eyebrow, Title and Tab Switcher */}
      <header className="games-header">
        <div>
          <p className="games-eyebrow">
            <Gamepad2 size={14} /> MEZON GAMING ARENA
          </p>
          <h1>Đấu trường trò chơi từ vựng</h1>
          <p className="games-subtitle">
            Rèn luyện từ vựng, phản xạ nghe và ngữ pháp tiếng Anh thông qua 6 mini-game tương tác. Tích lũy điểm số, thăng hạng Leaderboard và gom thưởng XP mỗi ngày!
          </p>
        </div>

        {/* Tab Switcher - Fixed high contrast text & active background */}
        {!activeGame && (
          <div className="games-tabs" role="tablist">
            <button
              type="button"
              className={`games-tab-btn ${activeTab === 'catalog' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('catalog')}
            >
              <Gamepad2 size={15} /> Kho trò chơi
            </button>
            <button
              type="button"
              className={`games-tab-btn ${activeTab === 'leaderboard' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('leaderboard')}
            >
              <Trophy size={15} /> Bảng xếp hạng
            </button>
          </div>
        )}
      </header>

      {/* Auth prompt banner for guest visitors */}
      {!authLoading && !isAuthenticated && !activeGame && (
        <div className="mb-6 p-4 rounded-2xl bg-amber-50 border border-amber-200 flex flex-wrap items-center justify-between gap-3 text-xs sm:text-sm text-amber-900">
          <span>
            <strong>Yêu cầu đăng nhập:</strong> Vui lòng đăng nhập Mezon SSO để tải bộ câu hỏi từ Database, lưu kỷ lục và nhận thưởng XP trong Game Center.
          </span>
          <Link
            href="/auth"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[var(--olive,#69734f)] text-white font-semibold hover:opacity-90 transition-all"
          >
            Đăng nhập ngay <ArrowRight size={14} />
          </Link>
        </div>
      )}

      {/* 2. Personal Stats Overview */}
      {!activeGame && (
        <section className="games-stats-grid" aria-label="Thống kê người chơi">
          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Gamepad2 size={20} />
            </div>
            <div className="games-stat-info">
              <small>Tổng trận đã chơi</small>
              <strong>{stats.gamesPlayed}</strong>
              <span>Đang hoạt động</span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Trophy size={20} />
            </div>
            <div className="games-stat-info">
              <small>Tổng trận thắng</small>
              <strong>{stats.gamesWon}</strong>
              <span>Tỉ lệ {Math.round((stats.gamesWon / Math.max(stats.gamesPlayed, 1)) * 100)}%</span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Zap size={20} />
            </div>
            <div className="games-stat-info">
              <small>Tổng XP thu về</small>
              <strong>+{stats.totalXp} XP</strong>
              <span>Tích lũy tài khoản</span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Flame size={20} />
            </div>
            <div className="games-stat-info">
              <small>Hạng kỷ lục</small>
              <strong>{stats.rankTitle}</strong>
              <span>Huy hiệu danh dự</span>
            </div>
          </div>
        </section>
      )}

      {/* 3. Main Views */}
      {activeGame ? (
        <ActiveGameArena
          game={activeGame}
          level={selectedLevel}
          onExit={() => setActiveGame(null)}
          onFinish={(won, xp) => recordGameCompletion(won, xp)}
        />
      ) : activeTab === 'catalog' ? (
        <section>
          {/* CEFR Level Filter Bar */}
          <div className="games-filter-bar">
            <div className="games-level-filter">
              <button
                className={`games-level-btn ${selectedLevel === 'all' ? 'is-active' : ''}`}
                onClick={() => setSelectedLevel('all')}
              >
                Mọi cấp độ
              </button>
              {['A1', 'A2', 'B1', 'B2'].map((lvl) => (
                <button
                  key={lvl}
                  className={`games-level-btn ${selectedLevel === lvl ? 'is-active' : ''}`}
                  onClick={() => setSelectedLevel(lvl)}
                >
                  Cấp độ {lvl}
                </button>
              ))}
            </div>
            <span className="text-xs text-[#77786f]">
              Hiển thị {filteredCatalog.length} trò chơi
            </span>
          </div>

          {/* Catalog Cards Grid */}
          <div className="games-grid">
            {filteredCatalog.map((item) => {
              const IconComp = item.icon;
              return (
                <article key={item.id} className="game-catalog-card">
                  <div>
                    <div className="game-catalog-card-top">
                      <span className="game-catalog-badge">{item.level}</span>
                      <span className="game-catalog-time">
                        <Clock size={13} /> {item.timeLimit}
                      </span>
                    </div>

                    <div className="game-catalog-body">
                      <div className="game-catalog-icon-title">
                        <div className="game-catalog-icon">
                          <IconComp size={22} />
                        </div>
                        <div>
                          <h3>{item.title}</h3>
                          <p>{item.englishTitle}</p>
                        </div>
                      </div>
                      <p className="game-catalog-desc">{item.desc}</p>
                    </div>
                  </div>

                  <div className="game-catalog-footer">
                    <span className="game-catalog-xp">
                      <Zap size={14} /> {item.xpReward}
                    </span>
                    <button
                      className="game-catalog-play-btn"
                      onClick={() => setActiveGame(item)}
                    >
                      Chơi ngay <ArrowRight size={14} />
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        </section>
      ) : (
        /* Leaderboard Tab */
        <GamesLeaderboardTab />
      )}
    </main>
  );
}

// ==========================================
// 3. ACTIVE GAME ARENA CONTAINER (DB SYNCED)
// ==========================================

function ActiveGameArena({
  game,
  level,
  onExit,
  onFinish,
}: {
  game: GameCatalogItem;
  level: string;
  onExit: () => void;
  onFinish: (won: boolean, xp: number) => void;
}) {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [dbData, setDbData] = useState<GameDataPayload | null>(null);
  const [gameOver, setGameOver] = useState(false);
  const [replayKey, setReplayKey] = useState(0);
  const [finalScore, setFinalScore] = useState(0);
  const [finalAccuracy, setFinalAccuracy] = useState(100);
  const [earnedXp, setEarnedXp] = useState(25);

  // Fetch live game data from Database on start / replay
  useEffect(() => {
    let isMounted = true;
    setLoading(true);
    setLoadError(null);
    setDbData(null);

    getGameData(game.apiKey, level === 'all' ? undefined : level)
      .then((res) => {
        if (!isMounted) return;
        const d = res?.data;
        const hasData =
          Boolean(d) &&
          Boolean(d?.attemptId) &&
          (Boolean(d?.items?.length) ||
            Boolean(d?.questions?.length) ||
            Boolean(d?.words?.length) ||
            Boolean(d?.prompt));

        if (hasData && d) {
          setDbData(d);
          setLoadError(null);
        } else {
          setLoadError('Chưa có dữ liệu cho trò chơi này. Vui lòng thử lại sau.');
        }
      })
      .catch((err: Error & { status?: number }) => {
        if (isMounted) {
          console.warn('Could not fetch DB game data:', err);
          if (err?.status === 401) {
            setLoadError('Bạn cần đăng nhập tài khoản Mezon để chơi và lưu điểm.');
          } else {
            setLoadError('Chưa có dữ liệu cho trò chơi này. Vui lòng thử lại sau.');
          }
        }
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [game.apiKey, level, replayKey]);

  const handleGameEnd = (
    score: number,
    accuracy: number,
    xp: number,
    answersPayload?: { questionId: string; answer: string }[],
    timeRemainingSeconds: number = 0
  ) => {
    if (!dbData?.attemptId) return;

    setFinalScore(score);
    setFinalAccuracy(accuracy);
    setEarnedXp(xp);
    setGameOver(true);

    // Call backend submit with attemptId and real player answers
    submitGameResult({
      attemptId: dbData.attemptId,
      gameType: game.apiKey,
      timeRemainingSeconds: Math.max(0, Math.round(timeRemainingSeconds)),
      answers: answersPayload && answersPayload.length > 0 ? answersPayload : [],
    })
      .then((res) => {
        if (res) {
          const srvXp = res.xpEarned ?? xp;
          const srvScore = res.score ?? score;
          if (res.xpEarned !== undefined) setEarnedXp(res.xpEarned);
          if (res.score !== undefined) setFinalScore(res.score);
          if (res.accuracy !== undefined) {
            const pct = res.accuracy <= 1 ? res.accuracy * 100 : res.accuracy;
            setFinalAccuracy(Math.round(pct));
          }
          onFinish(srvScore > 0, srvXp);
        } else {
          onFinish(score > 0, xp);
        }
      })
      .catch((err) => {
        console.error('Failed to submit game result:', err);
        onFinish(score > 0, xp);
      });
  };

  if (loading) {
    return (
      <div className="game-stage-box text-center py-16">
        <div className="quiz-spinner mx-auto mb-4" />
        <p className="text-sm font-semibold text-[#55574f]">Đang kết nối Database và tải câu hỏi...</p>
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="game-stage-box text-center py-14">
        <X size={36} className="text-[#bd684d] mx-auto mb-3" />
        <h3 className="text-base font-bold text-[#252820] mb-2">{loadError}</h3>
        <p className="text-xs text-[#77786f] mb-6">
          Hệ thống không tìm thấy gói câu hỏi phù hợp từ máy chủ hoặc bạn cần đăng nhập.
        </p>
        <button className="quiz-secondary inline-flex items-center gap-2" onClick={onExit}>
          <ArrowLeft size={15} /> Quay lại kho trò chơi
        </button>
      </div>
    );
  }

  if (gameOver) {
    return (
      <section className="quiz-results">
        <div className="quiz-result-icon">
          <Trophy size={36} />
        </div>
        <h2>Tuyệt vời! Hoàn thành trò chơi</h2>
        <p className="text-sm text-[#77786f] mt-1">
          Dữ liệu đã được đồng bộ với máy chủ và ghi nhận điểm số của bạn.
        </p>

        <div className="quiz-result-stats">
          <div>
            <span>ĐIỂM SỐ</span>
            <strong>{finalScore} pts</strong>
          </div>
          <div>
            <span>XP NHẬN ĐƯỢC</span>
            <strong className="text-[#69734f]">+{earnedXp} XP</strong>
          </div>
          <div>
            <span>ĐỘ CHÍNH XÁC</span>
            <strong>{finalAccuracy}%</strong>
          </div>
        </div>

        <div className="quiz-result-actions">
          <button
            className="quiz-primary"
            onClick={() => {
              setGameOver(false);
              setReplayKey((k) => k + 1);
            }}
          >
            <RotateCcw size={15} /> Chơi lại lần nữa
          </button>
          <button className="quiz-secondary" onClick={onExit}>
            <ArrowLeft size={15} /> Về kho trò chơi
          </button>
        </div>
      </section>
    );
  }

  return (
    <div className="game-arena-wrapper">
      {game.id === 'word-match' && (
        <WordMatchEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'scramble' && (
        <ScrambleEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'typing-race' && (
        <TypingRaceEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'word-fill' && (
        <ContextClozeEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'audio-reflex' && (
        <AudioReflexEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'word-guess' && (
        <WordleEngine dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
    </div>
  );
}

// ==========================================
// 4. ENGINE 1: WORD MATCH (DB POWERED)
// ==========================================

function WordMatchEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[], timeRemainingSeconds?: number) => void;
  onExit: () => void;
}) {
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [combo, setCombo] = useState(0);
  const [cards, setCards] = useState<
    { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[]
  >([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mismatchIds, setMismatchIds] = useState<string[]>([]);
  const matchedAnswersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const endedRef = useRef(false);

  // Init cards from DB items
  useEffect(() => {
    const rawItems = dbData?.items || [];
    if (rawItems.length === 0) return;

    matchedAnswersRef.current = [];
    endedRef.current = false;
    const generated: { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[] = [];
    rawItems.slice(0, 8).forEach((item) => {
      generated.push({ id: `en-${item.id}`, text: item.targetText, pairId: item.id, type: 'en', isMatched: false });
      generated.push({ id: `vi-${item.id}`, text: item.translation, pairId: item.id, type: 'vi', isMatched: false });
    });
    setCards(generated.sort(() => 0.5 - Math.random()));
  }, [dbData]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.min(100, Math.round((matchedAnswersRef.current.length / Math.max(cards.length / 2, 1)) * 100)), 25, matchedAnswersRef.current, 0);
      }
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, cards.length, onEnd]);

  // Click card handler
  const handleCardClick = (cardId: string) => {
    const card = cards.find((c) => c.id === cardId);
    if (!card || card.isMatched || mismatchIds.length > 0) return;

    if (!selectedId) {
      setSelectedId(cardId);
      return;
    }

    if (selectedId === cardId) {
      setSelectedId(null);
      return;
    }

    const prevCard = cards.find((c) => c.id === selectedId);
    if (!prevCard) {
      setSelectedId(cardId);
      return;
    }

    // Check if match
    if (prevCard.pairId === card.pairId && prevCard.type !== card.type) {
      const viCard = prevCard.type === 'vi' ? prevCard : card;
      if (!matchedAnswersRef.current.some((a) => a.questionId === card.pairId)) {
        matchedAnswersRef.current = [...matchedAnswersRef.current, { questionId: card.pairId, answer: viCard.text }];
      }
      setCards((all) =>
        all.map((c) => (c.pairId === card.pairId ? { ...c, isMatched: true } : c))
      );
      const newCombo = combo + 1;
      const nextScore = score + 25 * Math.min(newCombo, 3);
      setCombo(newCombo);
      setScore(nextScore);
      setSelectedId(null);

      // Check if all matched
      const remaining = cards.filter((c) => !c.isMatched && c.pairId !== card.pairId);
      if (remaining.length === 0 && !endedRef.current) {
        endedRef.current = true;
        setTimeout(() => onEnd(nextScore, 100, 35, matchedAnswersRef.current, timeLeft), 400);
      }
    } else {
      setMismatchIds([selectedId, cardId]);
      setCombo(0);
      setTimeout(() => {
        setMismatchIds([]);
        setSelectedId(null);
      }, 700);
    }
  };

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">Nối từ siêu tốc</div>
        <div className="game-arena-metrics">
          <span className={`game-arena-metric-pill timer ${timeLeft <= 10 ? 'urgent' : ''}`}>
            <Clock size={14} /> {timeLeft}s
          </span>
          <span className="game-arena-metric-pill">
            <Zap size={14} className="text-[#a9533c]" /> {score} pts
          </span>
          {combo > 1 && (
            <span className="game-arena-metric-pill bg-[#f5fbe9] text-[#315d1d] border-[#89a95e]">
              Combo x{combo}
            </span>
          )}
        </div>
      </div>

      <div className="game-stage-box">
        <div className="word-match-grid">
          {cards.map((c) => {
            const isSel = selectedId === c.id;
            const isMis = mismatchIds.includes(c.id);
            return (
              <button
                key={c.id}
                disabled={c.isMatched}
                onClick={() => handleCardClick(c.id)}
                className={`word-match-card ${isSel ? 'is-selected' : ''} ${
                  c.isMatched ? 'is-matched' : ''
                } ${isMis ? 'is-mismatched' : ''}`}
              >
                <span>{c.text}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 5. ENGINE 2: SENTENCE SCRAMBLE (DB POWERED)
// ==========================================

function ScrambleEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[], timeRemainingSeconds?: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [slottedTokens, setSlottedTokens] = useState<string[]>([]);
  const [availableTokens, setAvailableTokens] = useState<{ id: string; text: string }[]>([]);
  const [feedback, setFeedback] = useState<'correct' | 'wrong' | null>(null);
  const [wrongAttempts, setWrongAttempts] = useState(0);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const endedRef = useRef(false);

  const sentences = useMemo(() => {
    const fromDb = dbData?.questions || [];
    return fromDb.map((q) => ({
      id: q.id,
      prompt: q.prompt,
      tokens: q.tokens || (q.answer || q.prompt).split(' '),
      answer: q.answer || q.prompt,
    }));
  }, [dbData]);

  const currentSentence = sentences[currentIndex % (sentences.length || 1)];

  // Load sentence tokens
  useEffect(() => {
    if (!currentSentence) return;
    const shuffled = [...currentSentence.tokens]
      .map((t, idx) => ({ id: `${idx}-${t}`, text: t }))
      .sort(() => 0.5 - Math.random());
    setAvailableTokens(shuffled);
    setSlottedTokens([]);
    setFeedback(null);
    setWrongAttempts(0);
  }, [currentIndex, currentSentence]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.round((answersRef.current.length / Math.max(sentences.length, 1)) * 100), 30, answersRef.current, 0);
      }
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, sentences.length, onEnd]);

  const handleSlotToken = (item: { id: string; text: string }) => {
    setFeedback(null);
    setSlottedTokens((prev) => [...prev, item.text]);
    setAvailableTokens((prev) => prev.filter((t) => t.id !== item.id));
  };

  const handleUnslotToken = (text: string, index: number) => {
    setFeedback(null);
    setSlottedTokens((prev) => prev.filter((_, i) => i !== index));
    setAvailableTokens((prev) => [...prev, { id: `${Date.now()}-${text}`, text }]);
  };

  const handleResetSentence = () => {
    if (!currentSentence) return;
    setAvailableTokens(
      [...currentSentence.tokens].map((t, idx) => ({ id: `${Date.now()}-${idx}-${t}`, text: t })).sort(() => 0.5 - Math.random())
    );
    setSlottedTokens([]);
    setFeedback(null);
  };

  const handleSkipSentence = () => {
    if (currentIndex + 1 >= sentences.length) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.round((answersRef.current.length / Math.max(sentences.length, 1)) * 100), 30, answersRef.current, timeLeft);
      }
    } else {
      setCurrentIndex((i) => i + 1);
    }
  };

  const handleVerify = () => {
    if (!currentSentence) return;
    const rawUserAns = slottedTokens.join(' ').trim();
    const userAns = rawUserAns.toLowerCase();
    const correctAns = currentSentence.answer.trim().toLowerCase();
    const isCorrect = userAns === correctAns;

    if (isCorrect) {
      setFeedback('correct');
      const nextScore = score + 30;
      setScore(nextScore);
      answersRef.current = [...answersRef.current.filter((a) => a.questionId !== currentSentence.id), { questionId: currentSentence.id, answer: rawUserAns }];
      setTimeout(() => {
        if (currentIndex + 1 >= sentences.length) {
          if (!endedRef.current) {
            endedRef.current = true;
            onEnd(nextScore, 100, 35, answersRef.current, timeLeft);
          }
        } else {
          setCurrentIndex((i) => i + 1);
        }
      }, 750);
    } else {
      setWrongAttempts((n) => n + 1);
      setFeedback('wrong');
    }
  };

  if (!currentSentence) return null;

  const answerWords = currentSentence.answer.trim().split(/\s+/);
  const firstWord = answerWords[0] || '';
  const lastWord = answerWords[answerWords.length - 1] || '';

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Xếp câu chuẩn ({currentIndex + 1}/{sentences.length})
        </div>
        <div className="game-arena-metrics">
          <span className={`game-arena-metric-pill timer ${timeLeft <= 10 ? 'urgent' : ''}`}>
            <Clock size={14} /> {timeLeft}s
          </span>
          <span className="game-arena-metric-pill">
            <Zap size={14} className="text-[#a9533c]" /> {score} pts
          </span>
        </div>
      </div>

      <div className="game-stage-box">
        <div className="sentence-prompt-text">
          <p>Gợi ý nghĩa tiếng Việt — Sắp xếp thành câu tiếng Anh đúng</p>
          <h3>“{currentSentence.prompt}”</h3>
        </div>

        <div
          className={`sentence-drop-area ${
            feedback === 'correct'
              ? 'is-correct'
              : feedback === 'wrong'
              ? 'is-wrong'
              : ''
          }`}
        >
          {slottedTokens.length === 0 ? (
            <span className="text-xs text-[#8a897f]">Bấm các từ bên dưới để xếp vào đây...</span>
          ) : (
            slottedTokens.map((token, idx) => (
              <button
                key={idx}
                className="sentence-chip-btn is-slotted"
                onClick={() => handleUnslotToken(token, idx)}
              >
                {token}
              </button>
            ))
          )}
        </div>

        {feedback === 'correct' && (
          <div className="sentence-feedback-msg is-correct">
            Chính xác! +30 điểm — Đang chuyển sang câu tiếp theo...
          </div>
        )}
        {feedback === 'wrong' && (
          <div className="sentence-feedback-msg is-wrong">
            {availableTokens.length > 0
              ? `Bạn chưa xếp hết các từ (còn ${availableTokens.length} từ bên dưới).`
              : wrongAttempts >= 2
              ? `Thứ tự chưa khớp đáp án chuẩn — Gợi ý: câu bắt đầu bằng "${firstWord}" và kết thúc bằng "${lastWord}".`
              : 'Thứ tự từ chưa đúng đáp án chuẩn. Bấm vào từ ở khung trên để gỡ ra và xếp lại!'}
          </div>
        )}

        <div className="sentence-token-pool">
          {availableTokens.map((item) => (
            <button
              key={item.id}
              className="sentence-chip-btn"
              onClick={() => handleSlotToken(item)}
            >
              {item.text}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center justify-center gap-3 mt-6">
          <button
            type="button"
            className="quiz-secondary"
            disabled={slottedTokens.length === 0}
            onClick={handleResetSentence}
          >
            <RotateCcw size={15} /> Xếp lại
          </button>
          <button
            type="button"
            className="quiz-primary"
            style={{ marginLeft: 0 }}
            disabled={slottedTokens.length === 0}
            onClick={handleVerify}
          >
            <Check size={16} /> Kiểm tra câu
          </button>
          <button
            type="button"
            className="quiz-secondary"
            onClick={handleSkipSentence}
          >
            Bỏ qua <ArrowRight size={15} />
          </button>
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 6. ENGINE 3: TYPING RACE (DB POWERED)
// ==========================================

function TypingRaceEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[], timeRemainingSeconds?: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [inputVal, setInputVal] = useState('');
  const [wordsDone, setWordsDone] = useState(0);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const endedRef = useRef(false);

  const wordList = useMemo(() => {
    const raw = dbData?.items || dbData?.words || [];
    return raw.map((w) => ({
      id: w.id,
      en: w.targetText,
      ipa: w.phonetic || '',
      vi: w.translation,
    }));
  }, [dbData]);

  const currentWord = wordList[currentIndex % (wordList.length || 1)];

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.round((answersRef.current.length / Math.max(wordList.length, 1)) * 100), 40, answersRef.current, 0);
      }
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, wordList.length, onEnd]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!currentWord) return;
    const val = e.target.value;
    setInputVal(val);

    if (val.trim().toLowerCase() === currentWord.en.toLowerCase()) {
      const nextScore = score + 40;
      setScore(nextScore);
      setWordsDone((w) => w + 1);
      setInputVal('');
      answersRef.current = [...answersRef.current.filter((a) => a.questionId !== currentWord.id), { questionId: currentWord.id, answer: val.trim() }];
      if (currentIndex + 1 >= wordList.length) {
        if (!endedRef.current) {
          endedRef.current = true;
          onEnd(nextScore, 100, 40, answersRef.current, timeLeft);
        }
      } else {
        setCurrentIndex((i) => i + 1);
      }
    }
  };

  if (!currentWord) return null;

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Đua tốc độ gõ ({wordsDone} từ hoàn thành)
        </div>
        <div className="game-arena-metrics">
          <span className={`game-arena-metric-pill timer ${timeLeft <= 10 ? 'urgent' : ''}`}>
            <Clock size={14} /> {timeLeft}s
          </span>
          <span className="game-arena-metric-pill">
            <Zap size={14} className="text-[#a9533c]" /> {score} pts
          </span>
        </div>
      </div>

      <div className="game-stage-box">
        <div className="typing-target-box">
          <h2 className="typing-target-word">{currentWord.en}</h2>
          {currentWord.ipa && <p className="typing-target-ipa">{currentWord.ipa}</p>}
          <p className="typing-target-meaning">Nghĩa: {currentWord.vi}</p>
        </div>

        <input
          type="text"
          autoFocus
          className="typing-input-field"
          placeholder="Gõ lại chính xác từ vựng trên..."
          value={inputVal}
          onChange={handleChange}
        />
        <p className="text-center text-xs text-[#77786f] mt-3">
          Hệ thống sẽ tự động chuyển từ tiếp theo ngay khi bạn gõ đúng!
        </p>
      </div>
    </div>
  );
}

// ==========================================
// 7. ENGINE 4: CONTEXT CLOZE (DB POWERED)
// ==========================================

function ContextClozeEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[], timeRemainingSeconds?: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedChoice, setSelectedChoice] = useState<number | null>(null);
  const [showExplanation, setShowExplanation] = useState(false);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const correctCountRef = useRef(0);
  const endedRef = useRef(false);

  const questions = useMemo(() => {
    const raw = dbData?.questions || [];
    return raw.map((q) => {
      let choicesArr: string[] = [];
      const rawChoices = (q as any).choices;
      if (Array.isArray(rawChoices)) {
        choicesArr = rawChoices.map((c: any) =>
          typeof c === 'string' ? c : c.targetText || c.translation || String(c)
        );
      } else if (typeof rawChoices === 'string') {
        try {
          const parsed = JSON.parse(rawChoices);
          choicesArr = Array.isArray(parsed) ? parsed : rawChoices.trim().split(/\s+/);
        } catch {
          choicesArr = rawChoices.trim().split(/\s+/);
        }
      } else {
        choicesArr = ['A', 'B', 'C', 'D'];
      }

      return {
        id: q.id,
        prompt: q.prompt,
        choices: choicesArr,
        correctAnswer: q.answer || '',
        level: q.level || 'A2',
      };
    });
  }, [dbData]);

  const currentQ = questions[currentIndex % (questions.length || 1)];

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        const acc = Math.round((correctCountRef.current / Math.max(answersRef.current.length, 1)) * 100);
        onEnd(score, acc, 20, answersRef.current, 0);
      }
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleSelect = (idx: number) => {
    if (selectedChoice !== null || !currentQ) return;
    setSelectedChoice(idx);
    setShowExplanation(true);
    const chosenText = currentQ.choices[idx] || '';
    answersRef.current = [...answersRef.current.filter((a) => a.questionId !== currentQ.id), { questionId: currentQ.id, answer: chosenText }];
    const isRight = !currentQ.correctAnswer || chosenText.trim().toLowerCase() === currentQ.correctAnswer.trim().toLowerCase();
    if (isRight) {
      correctCountRef.current += 1;
      setScore((s) => s + 20);
    }
  };

  const handleNext = () => {
    if (currentIndex + 1 >= questions.length) {
      if (!endedRef.current) {
        endedRef.current = true;
        const acc = Math.round((correctCountRef.current / Math.max(questions.length, 1)) * 100);
        onEnd(score, acc, 25, answersRef.current, timeLeft);
      }
    } else {
      setCurrentIndex((i) => i + 1);
      setSelectedChoice(null);
      setShowExplanation(false);
    }
  };

  if (!currentQ) return null;

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Điền khuyết ngữ cảnh ({currentIndex + 1}/{questions.length})
        </div>
        <div className="game-arena-metrics">
          <span className={`game-arena-metric-pill timer ${timeLeft <= 10 ? 'urgent' : ''}`}>
            <Clock size={14} /> {timeLeft}s
          </span>
          <span className="game-arena-metric-pill">
            <Zap size={14} className="text-[#a9533c]" /> {score} pts
          </span>
        </div>
      </div>

      <div className="quiz-question">
        <div className="quiz-question-meta">
          <span className="study-level">{currentQ.level}</span>
          <span>Chọn đáp án đúng nhất để hoàn thiện câu</span>
        </div>

        <h2 className="my-6">{currentQ.prompt}</h2>

        <div className="quiz-options">
          {currentQ.choices.map((choice, idx) => {
            const isChosen = selectedChoice === idx;
            const isRightChoice = Boolean(currentQ.correctAnswer) && choice.trim().toLowerCase() === currentQ.correctAnswer.trim().toLowerCase();
            const showRight = selectedChoice !== null && (currentQ.correctAnswer ? isRightChoice : isChosen);
            const showWrong = isChosen && Boolean(currentQ.correctAnswer) && !isRightChoice;
            return (
              <button
                key={idx}
                disabled={selectedChoice !== null}
                className={`${showRight ? 'is-right' : ''} ${showWrong ? 'is-chosen' : ''}`}
                onClick={() => handleSelect(idx)}
              >
                <span className="quiz-option-key">
                  {String.fromCharCode(65 + idx)}
                </span>
                <span>{choice}</span>
                {showRight && <Check size={16} className="text-[#315d1d]" />}
                {showWrong && <X size={16} className="text-[#b4422e]" />}
              </button>
            );
          })}
        </div>

        {showExplanation && (
          <div className="quiz-explanation">
            <strong>{currentQ.correctAnswer ? `Đáp án đúng: ${currentQ.correctAnswer}` : 'Đã ghi nhận đáp án:'}</strong>
            <p>Điểm số và XP cuối cùng sẽ được máy chủ xác thực khi kết thúc lượt chơi.</p>
          </div>
        )}

        {selectedChoice !== null && (
          <div className="flex justify-end mt-5">
            <button className="quiz-primary" onClick={handleNext}>
              Câu tiếp theo <ArrowRight size={15} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

// ==========================================
// 8. ENGINE 5: AUDIO REFLEX (DB POWERED)
// ==========================================

function AudioReflexEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[], timeRemainingSeconds?: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const correctCountRef = useRef(0);
  const endedRef = useRef(false);

  const questions = useMemo(() => {
    const raw = dbData?.questions || [];
    return raw.map((q: any) => {
      let rawChoices = q.choices || [];
      if (typeof rawChoices === 'string') {
        try {
          rawChoices = JSON.parse(rawChoices);
        } catch {
          rawChoices = rawChoices.split(',');
        }
      }
      const choicesList: { label: string; targetText: string }[] = Array.isArray(rawChoices)
        ? rawChoices.map((c: any) =>
            typeof c === 'object' && c !== null
              ? { label: c.translation || c.targetText || '', targetText: c.targetText || c.translation || '' }
              : { label: String(c), targetText: String(c) }
          )
        : [];

      const correctObj = choicesList.find((c) => c.targetText === q.audioText);
      const correctChoice = correctObj?.label || choicesList[0]?.label || 'đúng';

      return {
        id: q.id,
        audioText: q.audioText || '',
        choices: choicesList,
        correctChoice,
      };
    });
  }, [dbData]);

  const currentQ = questions[currentIndex % (questions.length || 1)];

  // Speech function using Web Speech API
  const playAudio = useCallback((text: string) => {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window && text) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'en-US';
      utterance.rate = 0.85;
      window.speechSynthesis.speak(utterance);
    }
  }, []);

  useEffect(() => {
    if (!currentQ || !currentQ.audioText) return;
    setSelectedIdx(null);
    playAudio(currentQ.audioText);
  }, [currentIndex, currentQ, playAudio]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        const acc = Math.round((correctCountRef.current / Math.max(answersRef.current.length, 1)) * 100);
        onEnd(score, acc, 35, answersRef.current, 0);
      }
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleChoose = (idx: number) => {
    if (selectedIdx !== null || !currentQ) return;
    setSelectedIdx(idx);

    const chosen = currentQ.choices[idx];
    if (chosen) {
      answersRef.current = [...answersRef.current.filter((a) => a.questionId !== currentQ.id), { questionId: currentQ.id, answer: chosen.targetText }];
    }
    const isMatch = chosen?.label === currentQ.correctChoice;
    const nextScore = isMatch ? score + 35 : score;
    if (isMatch) {
      correctCountRef.current += 1;
      setScore(nextScore);
    }

    setTimeout(() => {
      if (currentIndex + 1 >= questions.length) {
        if (!endedRef.current) {
          endedRef.current = true;
          const acc = Math.round((correctCountRef.current / Math.max(questions.length, 1)) * 100);
          onEnd(nextScore, acc, 35, answersRef.current, timeLeft);
        }
      } else {
        setCurrentIndex((i) => i + 1);
      }
    }, 700);
  };

  if (!currentQ) return null;

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Phản xạ nghe âm ({currentIndex + 1}/{questions.length})
        </div>
        <div className="game-arena-metrics">
          <span className={`game-arena-metric-pill timer ${timeLeft <= 10 ? 'urgent' : ''}`}>
            <Clock size={14} /> {timeLeft}s
          </span>
          <span className="game-arena-metric-pill">
            <Zap size={14} className="text-[#a9533c]" /> {score} pts
          </span>
        </div>
      </div>

      <div className="game-stage-box text-center">
        <p className="text-xs text-[#77786f] uppercase tracking-wider font-semibold mb-2">
          Lắng nghe phát âm và chọn nghĩa tiếng Việt chuẩn xác
        </p>

        <button
          className="audio-reflex-speaker-btn"
          onClick={() => playAudio(currentQ.audioText)}
          aria-label="Phát lại âm thanh"
        >
          <Volume2 size={36} />
        </button>

        <p className="text-xs text-[#8a897f] mb-6">Bấm vào biểu tượng loa để nghe lại âm thanh</p>

        <div className="quiz-options">
          {currentQ.choices.map((opt, idx: number) => {
            const isRight = selectedIdx !== null && opt.label === currentQ.correctChoice;
            const isWrong = selectedIdx === idx && opt.label !== currentQ.correctChoice;
            return (
              <button
                key={idx}
                disabled={selectedIdx !== null}
                className={`${isRight ? 'is-right' : ''} ${isWrong ? 'is-chosen' : ''}`}
                onClick={() => handleChoose(idx)}
              >
                <span className="quiz-option-key">{idx + 1}</span>
                <span>{opt.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 9. ENGINE 6: WORDLE (SERVER EVALUATED)
// ==========================================

function WordleEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[]) => void;
  onExit: () => void;
}) {
  const [guesses, setGuesses] = useState<string[]>([]);
  const [evaluations, setEvaluations] = useState<('correct' | 'present' | 'absent')[][]>([]);
  const [currentGuess, setCurrentGuess] = useState('');
  const [isGameOver, setIsGameOver] = useState(false);
  const [statusMsg, setStatusMsg] = useState('');
  const [keyColors, setKeyColors] = useState<Record<string, 'correct' | 'present' | 'absent'>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const answersBuffer = useRef<{ questionId: string; answer: string }[]>([]);

  const attemptId = dbData?.attemptId || '';
  const hint = dbData?.prompt || '';
  const category = dbData?.category || 'Từ vựng';
  const meaning = dbData?.meaningVi || '';

  const handleKey = useCallback(
    async (key: string) => {
      if (isGameOver || isSubmitting) return;

      if (key === 'ENTER') {
        if (currentGuess.length < 5) {
          setStatusMsg('Vui lòng nhập đủ 5 chữ cái');
          setTimeout(() => setStatusMsg(''), 1500);
          return;
        }

        if (!attemptId) {
          setStatusMsg('Thiếu mã phiên chơi từ máy chủ');
          return;
        }

        const guessWord = currentGuess.toUpperCase();
        setIsSubmitting(true);

        try {
          const res = await submitWordleGuess(attemptId, guessWord);
          if (!res.success) {
            setStatusMsg(res.message || 'Lỗi kiểm tra từ');
            setTimeout(() => setStatusMsg(''), 2000);
            setIsSubmitting(false);
            return;
          }

          answersBuffer.current.push({
            questionId: attemptId,
            answer: guessWord,
          });

          const nextGuesses = [...guesses, guessWord];
          const nextEvals = [...evaluations, res.evaluations];
          setGuesses(nextGuesses);
          setEvaluations(nextEvals);
          setCurrentGuess('');

          // Update keyboard key colors by priority: correct (3) > present (2) > absent (1)
          setKeyColors((prev) => {
            const updated = { ...prev };
            const priority: Record<string, number> = { correct: 3, present: 2, absent: 1 };
            for (let i = 0; i < 5; i++) {
              const letter = guessWord[i];
              const ev = res.evaluations[i];
              const cur = updated[letter];
              if (!cur || priority[ev] > priority[cur]) {
                updated[letter] = ev;
              }
            }
            return updated;
          });

          if (res.isWon) {
            setIsGameOver(true);
            setStatusMsg('Chúc mừng bạn đã đoán chính xác!');
            setTimeout(() => {
              onEnd(100, 100, 50, answersBuffer.current);
            }, 1200);
          } else if (res.isGameOver || nextGuesses.length >= 6) {
            setIsGameOver(true);
            setStatusMsg('Hết lượt đoán! Trò chơi kết thúc.');
            setTimeout(() => {
              onEnd(20, 40, 15, answersBuffer.current);
            }, 1500);
          }
        } catch (err: any) {
          setStatusMsg(err.message || 'Không thể kết nối máy chủ');
          setTimeout(() => setStatusMsg(''), 2000);
        } finally {
          setIsSubmitting(false);
        }
      } else if (key === 'BACK' || key === '⌫') {
        setCurrentGuess((prev) => prev.slice(0, -1));
      } else if (/^[A-Z]$/.test(key) && currentGuess.length < 5) {
        setCurrentGuess((prev) => prev + key);
      }
    },
    [attemptId, currentGuess, evaluations, guesses, isGameOver, isSubmitting, onEnd]
  );

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter') handleKey('ENTER');
      else if (e.key === 'Backspace') handleKey('BACK');
      else if (/^[a-zA-Z]$/.test(e.key)) handleKey(e.key.toUpperCase());
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleKey]);

  const KB_ROWS = [
    ['Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P'],
    ['A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L'],
    ['ENTER', 'Z', 'X', 'C', 'V', 'B', 'N', 'M', '⌫'],
  ];

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Lingo Wordle ({guesses.length}/6 lượt đoán)
        </div>
        <div className="game-arena-metrics">
          <span className="game-arena-metric-pill">
            <HelpCircle size={14} /> Chủ đề: {category}
          </span>
        </div>
      </div>

      <div className="game-stage-box">
        <div className="text-center mb-4">
          {hint && <p className="text-xs text-[#77786f]">Gợi ý: {hint}</p>}
          {meaning && <p className="text-xs font-semibold text-[#69734f] mt-1">Nghĩa: {meaning}</p>}
          {statusMsg && (
            <p className="text-xs font-bold text-[#bd684d] mt-2 animate-bounce">{statusMsg}</p>
          )}
        </div>

        <div className="wordle-grid">
          {Array.from({ length: 6 }).map((_, rowIndex) => {
            const guessWord = guesses[rowIndex] || (rowIndex === guesses.length ? currentGuess : '');
            const rowEval = evaluations[rowIndex];
            const isSubmitted = rowIndex < guesses.length;

            return (
              <div key={rowIndex} className="wordle-row">
                {Array.from({ length: 5 }).map((_, colIndex) => {
                  const letter = guessWord[colIndex] || '';
                  const cellStatus = isSubmitted && rowEval?.[colIndex] ? rowEval[colIndex] : '';

                  return (
                    <div
                      key={colIndex}
                      className={`wordle-cell ${letter ? 'filled' : ''} ${cellStatus}`}
                    >
                      {letter}
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>

        <div className="wordle-keyboard mt-6">
          {KB_ROWS.map((row, rIdx) => (
            <div key={rIdx} className="wordle-kb-row">
              {row.map((k) => {
                const status = keyColors[k] || '';
                return (
                  <button
                    key={k}
                    disabled={isGameOver || isSubmitting}
                    className={`wordle-key-btn ${status}`}
                    onClick={() => handleKey(k)}
                  >
                    {k}
                  </button>
                );
              })}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 10. LEADERBOARD TAB (DB CONNECTED, NO MOCK)
// ==========================================

function GamesLeaderboardTab() {
  const [entries, setEntries] = useState<GameLeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getGameLeaderboard(undefined, 20)
      .then((res) => {
        if (res && res.entries && Array.isArray(res.entries)) {
          setEntries(res.entries);
        } else {
          setEntries([]);
        }
      })
      .catch(() => {
        setEntries([]);
      })
      .finally(() => setLoading(false));
  }, []);

  const topThree = entries.slice(0, 3);
  const remaining = entries.slice(3);

  if (loading) {
    return (
      <div className="game-stage-box text-center py-12">
        <div className="quiz-spinner mx-auto mb-3" />
        <p className="text-xs font-semibold text-[#55574f]">Đang tải bảng xếp hạng...</p>
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <div className="leaderboard-panel text-center py-12">
        <Trophy size={42} className="text-[#aeb89a] mx-auto mb-3" />
        <h3 className="text-base font-bold text-[#252820] mb-1">Chưa có người chơi</h3>
        <p className="text-xs text-[#77786f]">
          Chưa có dữ liệu bảng xếp hạng. Hãy là người đầu tiên tham gia mini-game để ghi danh!
        </p>
      </div>
    );
  }

  return (
    <section>
      {/* Podium Top 3 */}
      <div className="leaderboard-podium">
        {topThree[1] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#2</span>
            <strong>{topThree[1].displayName}</strong>
            <span>{topThree[1].highScore.toLocaleString('vi-VN')} pts</span>
          </div>
        )}

        {topThree[0] && (
          <div className="leaderboard-podium-card rank-1">
            <span className="leaderboard-medal">🏆</span>
            <strong>{topThree[0].displayName}</strong>
            <span>{topThree[0].highScore.toLocaleString('vi-VN')} pts</span>
          </div>
        )}

        {topThree[2] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#3</span>
            <strong>{topThree[2].displayName}</strong>
            <span>{topThree[2].highScore.toLocaleString('vi-VN')} pts</span>
          </div>
        )}
      </div>

      {/* Leaderboard Rows */}
      <div className="leaderboard-panel mt-6">
        <div className="leaderboard-panel-heading mb-4">
          <h2>Bảng vàng danh dự</h2>
          <span className="text-xs text-[#77786f]">Đồng bộ từ Database xếp hạng</span>
        </div>

        <div className="leaderboard-list">
          {remaining.map((entry) => (
            <div key={entry.userId} className="leaderboard-row">
              <span className="leaderboard-rank">#{entry.rank}</span>
              <div className="leaderboard-avatar">
                {entry.displayName.slice(0, 1)}
              </div>
              <div className="leaderboard-name">
                <span>{entry.displayName}</span>
                <small>Thắng {entry.totalWins} trận</small>
              </div>
              <span className="leaderboard-xp font-semibold">
                {entry.highScore.toLocaleString('vi-VN')} pts
              </span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
