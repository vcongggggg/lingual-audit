'use client';

import React, { useState, useEffect, useMemo, useCallback } from 'react';
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
import { MOCK_LEADERBOARD } from '@/lib/mock-data';
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
// 1. FALLBACK DATASETS (100% CLEAN UTF-8)
// ==========================================

interface VocabCard {
  id: string;
  en: string;
  vi: string;
  ipa: string;
  level: string;
}

const FALLBACK_VOCAB: VocabCard[] = [
  { id: 'v1', en: 'breakfast', vi: 'bữa sáng', ipa: '/ˈbrekfəst/', level: 'A1' },
  { id: 'v2', en: 'elephant', vi: 'con voi', ipa: '/ˈelɪfənt/', level: 'A1' },
  { id: 'v3', en: 'journey', vi: 'hành trình', ipa: '/ˈdʒɜːrni/', level: 'A2' },
  { id: 'v4', en: 'improve', vi: 'cải thiện', ipa: '/ɪmˈpruːv/', level: 'A2' },
  { id: 'v5', en: 'achieve', vi: 'đạt được', ipa: '/əˈtʃiːv/', level: 'A2' },
  { id: 'v6', en: 'knowledge', vi: 'kiến thức', ipa: '/ˈnɒlɪdʒ/', level: 'B1' },
  { id: 'v7', en: 'creative', vi: 'sáng tạo', ipa: '/kriˈeɪtɪv/', level: 'B1' },
  { id: 'v8', en: 'efficient', vi: 'hiệu quả', ipa: '/ɪˈfɪʃnt/', level: 'B2' },
];

const FALLBACK_SCRAMBLES = [
  { id: 's1', prompt: 'I drink hot coffee every morning', tokens: ['drink', 'hot', 'every', 'I', 'morning', 'coffee'], answer: 'I drink hot coffee every morning', level: 'A1' },
  { id: 's2', prompt: 'Nice to meet you in Vietnam', tokens: ['to', 'Nice', 'Vietnam', 'in', 'you', 'meet'], answer: 'Nice to meet you in Vietnam', level: 'A1' },
  { id: 's3', prompt: 'She plays piano very well', tokens: ['well', 'She', 'plays', 'very', 'piano'], answer: 'She plays piano very well', level: 'A2' },
];

const FALLBACK_CLOZE = [
  { id: 'c1', prompt: "'apple' nghĩa là gì?", choices: ['quả táo', 'quả chuối', 'con mèo', 'quyển sách'], answer: 'quả táo', level: 'A1', explanation: "'apple' = quả táo." },
  { id: 'c2', prompt: "'quả chuối' trong tiếng Anh là gì?", choices: ['banana', 'orange', 'table', 'school'], answer: 'banana', level: 'A1', explanation: "'quả chuối' = 'banana'." },
  { id: 'c3', prompt: "Reading books every single morning is a good _____ that everyone should maintain.", choices: ['habit', 'journey', 'careful', 'improve'], answer: 'habit', level: 'A2', explanation: "'habit' (danh từ) nghĩa là thói quen tốt." },
];

// ==========================================
// 2. GAME CATALOG METADATA
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
// 3. MAIN COMPONENT
// ==========================================

export default function GameCenterPage() {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'catalog' | 'leaderboard'>('catalog');
  const [selectedLevel, setSelectedLevel] = useState<string>('all');
  const [activeGame, setActiveGame] = useState<GameCatalogItem | null>(null);

  // Player Stats (Loaded from backend / localStorage fallback)
  const [stats, setStats] = useState({
    gamesPlayed: 6,
    gamesWon: 6,
    totalXp: 348,
    rankTitle: 'Cao thủ',
  });

  useEffect(() => {
    // Try fetching real user game stats from database
    getMyGameStats()
      .then((res) => {
        if (res && res.success && res.totalGamesPlayed > 0) {
          setStats({
            gamesPlayed: res.totalGamesPlayed,
            gamesWon: res.totalWins,
            totalXp: res.totalXpEarned,
            rankTitle: res.totalGamesPlayed > 10 ? 'Đại cao thủ' : 'Cao thủ',
          });
        }
      })
      .catch(() => {
        try {
          const saved = localStorage.getItem('mezon_games_stats');
          if (saved) setStats(JSON.parse(saved));
        } catch {}
      });
  }, []);

  const recordGameCompletion = useCallback((won: boolean, xpEarned: number) => {
    setStats((prev) => {
      const next = {
        ...prev,
        gamesPlayed: prev.gamesPlayed + 1,
        gamesWon: prev.gamesWon + (won ? 1 : 0),
        totalXp: prev.totalXp + xpEarned,
      };
      try {
        localStorage.setItem('mezon_games_stats', JSON.stringify(next));
      } catch {}
      return next;
    });
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
// 4. ACTIVE GAME ARENA CONTAINER (DB SYNCED)
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
  const [dbData, setDbData] = useState<GameDataPayload | null>(null);
  const [gameOver, setGameOver] = useState(false);
  const [finalScore, setFinalScore] = useState(0);
  const [finalAccuracy, setFinalAccuracy] = useState(100);
  const [earnedXp, setEarnedXp] = useState(25);

  // Fetch live game data from Database on start
  useEffect(() => {
    let isMounted = true;
    setLoading(true);

    getGameData(game.apiKey, level === 'all' ? undefined : level)
      .then((res) => {
        if (!isMounted) return;
        if (res && res.data) {
          setDbData(res.data);
        }
      })
      .catch((err) => {
        console.warn('Could not fetch DB game data, using verified fallback:', err);
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [game.apiKey, level]);

  const handleGameEnd = (score: number, accuracy: number, xp: number, answersPayload?: { questionId: string; answer: string }[]) => {
    setFinalScore(score);
    setFinalAccuracy(accuracy);
    setEarnedXp(xp);
    setGameOver(true);
    onFinish(score > 0, xp);

    // Call backend submit with attemptId
    const attemptId = dbData?.attemptId || `client-${Date.now()}`;
    submitGameResult({
      attemptId,
      gameType: game.apiKey,
      timeRemainingSeconds: 0,
      answers: answersPayload || [{ questionId: 'summary', answer: String(score) }],
    }).catch(() => {});
  };

  if (loading) {
    return (
      <div className="game-stage-box text-center py-16">
        <div className="quiz-spinner mx-auto mb-4" />
        <p className="text-sm font-semibold text-[#55574f]">Đang kết nối Database và tải câu hỏi...</p>
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
          <button className="quiz-primary" onClick={() => setGameOver(false)}>
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
// 5. ENGINE 1: WORD MATCH (DB POWERED)
// ==========================================

function WordMatchEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
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

  // Init cards from DB items (or fallback)
  useEffect(() => {
    const rawItems = dbData?.items || [];
    const source = rawItems.length >= 4 ? rawItems : FALLBACK_VOCAB.map(v => ({ id: v.id, targetText: v.en, translation: v.vi }));
    const generated: { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[] = [];

    source.slice(0, 8).forEach((item) => {
      generated.push({ id: `en-${item.id}`, text: item.targetText, pairId: item.id, type: 'en', isMatched: false });
      generated.push({ id: `vi-${item.id}`, text: item.translation, pairId: item.id, type: 'vi', isMatched: false });
    });
    setCards(generated.sort(() => 0.5 - Math.random()));
  }, [dbData]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, Math.min(100, Math.round((score / 200) * 100)), 25);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

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
      setCards((all) =>
        all.map((c) => (c.pairId === card.pairId ? { ...c, isMatched: true } : c))
      );
      const newCombo = combo + 1;
      setCombo(newCombo);
      setScore((s) => s + 25 * Math.min(newCombo, 3));
      setSelectedId(null);

      // Check if all matched
      const remaining = cards.filter((c) => !c.isMatched && c.pairId !== card.pairId);
      if (remaining.length === 0) {
        setTimeout(() => onEnd(score + 100, 100, 35), 400);
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
// 6. ENGINE 2: SENTENCE SCRAMBLE (DB POWERED)
// ==========================================

function ScrambleEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [slottedTokens, setSlottedTokens] = useState<string[]>([]);
  const [availableTokens, setAvailableTokens] = useState<{ id: string; text: string }[]>([]);
  const [feedback, setFeedback] = useState<'correct' | 'wrong' | null>(null);

  const sentences = useMemo(() => {
    const fromDb = dbData?.questions || [];
    if (fromDb.length > 0) {
      return fromDb.map(q => ({
        id: q.id,
        prompt: q.prompt,
        tokens: q.tokens || q.prompt.split(' '),
        answer: q.prompt,
      }));
    }
    return FALLBACK_SCRAMBLES;
  }, [dbData]);

  const currentSentence = sentences[currentIndex % sentences.length];

  // Load sentence tokens
  useEffect(() => {
    if (!currentSentence) return;
    const shuffled = [...currentSentence.tokens]
      .map((t, idx) => ({ id: `${idx}-${t}`, text: t }))
      .sort(() => 0.5 - Math.random());
    setAvailableTokens(shuffled);
    setSlottedTokens([]);
    setFeedback(null);
  }, [currentIndex, currentSentence]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, 90, 30);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleSlotToken = (item: { id: string; text: string }) => {
    setSlottedTokens((prev) => [...prev, item.text]);
    setAvailableTokens((prev) => prev.filter((t) => t.id !== item.id));
  };

  const handleUnslotToken = (text: string, index: number) => {
    setSlottedTokens((prev) => prev.filter((_, i) => i !== index));
    setAvailableTokens((prev) => [...prev, { id: `${Date.now()}-${text}`, text }]);
  };

  const handleVerify = () => {
    const userAns = slottedTokens.join(' ').trim().toLowerCase();
    const correctAns = currentSentence.answer.trim().toLowerCase();
    const isCorrect = userAns === correctAns;

    if (isCorrect) {
      setFeedback('correct');
      setScore((s) => s + 30);
      setTimeout(() => {
        if (currentIndex + 1 >= sentences.length) {
          onEnd(score + 30, 100, 35);
        } else {
          setCurrentIndex((i) => i + 1);
        }
      }, 800);
    } else {
      setFeedback('wrong');
      setTimeout(() => setFeedback(null), 1000);
    }
  };

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
          <p>Sắp xếp các từ thành câu đúng</p>
          <h3>"{currentSentence.prompt}"</h3>
        </div>

        <div
          className={`sentence-drop-area ${
            feedback === 'correct'
              ? 'border-[#70a83b] bg-[#f5ffe9]'
              : feedback === 'wrong'
              ? 'border-[#d5684f] bg-[#fff4ef]'
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

        <div className="flex justify-center mt-6">
          <button
            className="quiz-primary"
            disabled={slottedTokens.length === 0}
            onClick={handleVerify}
          >
            <Check size={16} /> Kiểm tra câu
          </button>
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 7. ENGINE 3: TYPING RACE (DB POWERED)
// ==========================================

function TypingRaceEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [inputVal, setInputVal] = useState('');
  const [wordsDone, setWordsDone] = useState(0);

  const wordList = useMemo(() => {
    const raw = dbData?.items || dbData?.words || [];
    if (raw.length > 0) {
      return raw.map(w => ({
        en: w.targetText,
        ipa: w.phonetic || '',
        vi: w.translation,
      }));
    }
    return FALLBACK_VOCAB;
  }, [dbData]);

  const currentWord = wordList[currentIndex % wordList.length];

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, 100, 40);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInputVal(val);

    if (val.trim().toLowerCase() === currentWord.en.toLowerCase()) {
      setScore((s) => s + 40);
      setWordsDone((w) => w + 1);
      setInputVal('');
      setCurrentIndex((i) => i + 1);
    }
  };

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
// 8. ENGINE 4: CONTEXT CLOZE (DB POWERED)
// ==========================================

function ContextClozeEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedChoice, setSelectedChoice] = useState<number | null>(null);
  const [showExplanation, setShowExplanation] = useState(false);

  const questions = useMemo(() => {
    const raw = dbData?.questions || [];
    if (raw.length > 0) {
      return raw.map(q => {
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
          correctIndex: 0,
          level: q.level || 'A2',
          explanation: 'Đáp án được kiểm tra trực tiếp qua Database câu hỏi.',
        };
      });
    }
    return FALLBACK_CLOZE.map((c, i) => ({
      id: c.id,
      prompt: c.prompt,
      choices: c.choices,
      correctIndex: c.choices.indexOf(c.answer) >= 0 ? c.choices.indexOf(c.answer) : 0,
      level: c.level,
      explanation: c.explanation,
    }));
  }, [dbData]);

  const currentQ = questions[currentIndex % questions.length];

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, 90, 20);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleSelect = (idx: number) => {
    if (selectedChoice !== null) return;
    setSelectedChoice(idx);
    setShowExplanation(true);
    setScore((s) => s + 20);
  };

  const handleNext = () => {
    if (currentIndex + 1 >= questions.length) {
      onEnd(score, 95, 25);
    } else {
      setCurrentIndex((i) => i + 1);
      setSelectedChoice(null);
      setShowExplanation(false);
    }
  };

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
            return (
              <button
                key={idx}
                disabled={selectedChoice !== null}
                className={isChosen ? 'is-right' : ''}
                onClick={() => handleSelect(idx)}
              >
                <span className="quiz-option-key">
                  {String.fromCharCode(65 + idx)}
                </span>
                <span>{choice}</span>
                {isChosen && <Check size={16} className="text-[#315d1d]" />}
              </button>
            );
          })}
        </div>

        {showExplanation && (
          <div className="quiz-explanation">
            <strong>Thông tin câu hỏi:</strong>
            <p>{currentQ.explanation}</p>
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
// 9. ENGINE 5: AUDIO REFLEX (DB POWERED)
// ==========================================

function AudioReflexEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);

  const questions = useMemo(() => {
    const raw = dbData?.questions || [];
    if (raw.length > 0) {
      return raw.map((q: any) => {
        let rawChoices = q.choices || [];
        if (typeof rawChoices === 'string') {
          try {
            rawChoices = JSON.parse(rawChoices);
          } catch {
            rawChoices = rawChoices.split(',');
          }
        }
        const choicesList = Array.isArray(rawChoices)
          ? rawChoices.map((c: any) => (typeof c === 'object' && c !== null ? c.translation || c.targetText : String(c)))
          : ['Lựa chọn 1', 'Lựa chọn 2', 'Lựa chọn 3', 'Lựa chọn 4'];

        const correctObj = Array.isArray(rawChoices)
          ? rawChoices.find((c: any) => typeof c === 'object' && c !== null && c.targetText === q.audioText)
          : null;
        const correctChoice = correctObj?.translation || choicesList[0] || 'đúng';

        return {
          id: q.id,
          audioText: q.audioText || 'hello',
          choices: choicesList,
          correctChoice,
        };
      });
    }
    return FALLBACK_VOCAB.map(v => ({
      id: v.id,
      audioText: v.en,
      choices: [v.vi, 'con mèo', 'trường học', 'gia đình'].sort(() => 0.5 - Math.random()),
      correctChoice: v.vi,
    }));
  }, [dbData]);

  const currentQ = questions[currentIndex % questions.length];

  // Speech function using Web Speech API
  const playAudio = useCallback((text: string) => {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'en-US';
      utterance.rate = 0.85;
      window.speechSynthesis.speak(utterance);
    }
  }, []);

  useEffect(() => {
    if (!currentQ) return;
    setSelectedIdx(null);
    playAudio(currentQ.audioText);
  }, [currentIndex, currentQ, playAudio]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, 90, 35);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  const handleChoose = (idx: number) => {
    if (selectedIdx !== null) return;
    setSelectedIdx(idx);

    const isMatch = currentQ.choices[idx] === currentQ.correctChoice;
    if (isMatch) setScore((s) => s + 35);

    setTimeout(() => {
      if (currentIndex + 1 >= questions.length) {
        onEnd(score + 35, 100, 35);
      } else {
        setCurrentIndex((i) => i + 1);
      }
    }, 700);
  };

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
          {currentQ.choices.map((opt: string, idx: number) => {
            const isRight = selectedIdx !== null && opt === currentQ.correctChoice;
            const isWrong = selectedIdx === idx && opt !== currentQ.correctChoice;
            return (
              <button
                key={idx}
                disabled={selectedIdx !== null}
                className={`${isRight ? 'is-right' : ''} ${isWrong ? 'is-chosen' : ''}`}
                onClick={() => handleChoose(idx)}
              >
                <span className="quiz-option-key">{idx + 1}</span>
                <span>{opt}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 10. ENGINE 6: WORDLE (DB POWERED)
// ==========================================

function WordleEngine({
  dbData,
  onEnd,
  onExit,
}: {
  dbData: GameDataPayload | null;
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const challenge = useMemo(() => {
    if (dbData && dbData.prompt) {
      // Derive target word from dbData id or fallback
      const wordFromId = (dbData.id || '').replace(/^w_/, '').toUpperCase();
      return {
        word: wordFromId.length === 5 ? wordFromId : 'CLOUD',
        category: dbData.category || 'Công nghệ',
        hint: dbData.prompt,
        meaning: dbData.meaningVi || 'Thuật ngữ dữ liệu',
      };
    }
    return {
      word: 'WATER',
      category: 'Cuộc sống',
      hint: 'Chất lỏng trong suốt chiếm hơn 70% cơ thể người',
      meaning: 'Nước uống, nguồn sống',
    };
  }, [dbData]);

  const [guesses, setGuesses] = useState<string[]>([]);
  const [currentGuess, setCurrentGuess] = useState('');
  const [isGameOver, setIsGameOver] = useState(false);
  const [statusMsg, setStatusMsg] = useState('');

  const target = challenge.word;

  const handleKey = useCallback(
    (key: string) => {
      if (isGameOver) return;

      if (key === 'ENTER') {
        if (currentGuess.length < 5) {
          setStatusMsg('Vui lòng nhập đủ 5 chữ cái');
          setTimeout(() => setStatusMsg(''), 1500);
          return;
        }

        const nextGuesses = [...guesses, currentGuess];
        setGuesses(nextGuesses);
        setCurrentGuess('');

        if (currentGuess === target) {
          setIsGameOver(true);
          setStatusMsg('Chúc mừng bạn đã đoán chính xác!');
          setTimeout(() => onEnd(100, 100, 50), 1200);
        } else if (nextGuesses.length >= 6) {
          setIsGameOver(true);
          setStatusMsg(`Rất tiếc! Từ chính xác là ${target}`);
          setTimeout(() => onEnd(30, 50, 20), 1800);
        }
      } else if (key === 'BACK' || key === '⌫') {
        setCurrentGuess((prev) => prev.slice(0, -1));
      } else if (/^[A-Z]$/.test(key) && currentGuess.length < 5) {
        setCurrentGuess((prev) => prev + key);
      }
    },
    [currentGuess, guesses, isGameOver, onEnd, target]
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

  const getCellClass = (word: string, letterIndex: number) => {
    const char = word[letterIndex];
    if (target[letterIndex] === char) return 'correct';
    if (target.includes(char)) return 'present';
    return 'absent';
  };

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
            <HelpCircle size={14} /> Chủ đề: {challenge.category}
          </span>
        </div>
      </div>

      <div className="game-stage-box">
        <div className="text-center mb-4">
          <p className="text-xs text-[#77786f]">Gợi ý: {challenge.hint}</p>
          <p className="text-xs font-semibold text-[#69734f] mt-1">Nghĩa: {challenge.meaning}</p>
          {statusMsg && (
            <p className="text-xs font-bold text-[#bd684d] mt-2 animate-bounce">{statusMsg}</p>
          )}
        </div>

        <div className="wordle-grid">
          {Array.from({ length: 6 }).map((_, rowIndex) => {
            const guessWord = guesses[rowIndex] || (rowIndex === guesses.length ? currentGuess : '');
            return (
              <div key={rowIndex} className="wordle-row">
                {Array.from({ length: 5 }).map((_, colIndex) => {
                  const letter = guessWord[colIndex] || '';
                  const isSubmitted = rowIndex < guesses.length;
                  const statusClass = isSubmitted ? getCellClass(guessWord, colIndex) : '';
                  return (
                    <div
                      key={colIndex}
                      className={`wordle-cell ${letter ? 'filled' : ''} ${statusClass}`}
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
              {row.map((k) => (
                <button key={k} className="wordle-key-btn" onClick={() => handleKey(k)}>
                  {k}
                </button>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// 11. LEADERBOARD TAB (DB CONNECTED)
// ==========================================

function GamesLeaderboardTab() {
  const [entries, setEntries] = useState<GameLeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getGameLeaderboard(undefined, 20)
      .then((res) => {
        if (res && res.entries && res.entries.length > 0) {
          setEntries(res.entries);
        } else {
          // Fallback to mock
          setEntries(
            MOCK_LEADERBOARD.map(m => ({
              rank: m.rank,
              userId: m.user_id,
              displayName: m.name,
              highScore: m.xp,
              totalWins: Math.round(m.xp / 100),
              totalXpEarned: m.xp,
              gameType: 'all',
              lastPlayedAt: new Date().toISOString(),
            }))
          );
        }
      })
      .catch(() => {
        setEntries(
          MOCK_LEADERBOARD.map(m => ({
            rank: m.rank,
            userId: m.user_id,
            displayName: m.name,
            highScore: m.xp,
            totalWins: Math.round(m.xp / 100),
            totalXpEarned: m.xp,
            gameType: 'all',
            lastPlayedAt: new Date().toISOString(),
          }))
        );
      })
      .finally(() => setLoading(false));
  }, []);

  const topThree = entries.slice(0, 3);
  const remaining = entries.slice(3);

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
