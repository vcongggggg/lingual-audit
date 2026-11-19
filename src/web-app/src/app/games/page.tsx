'use client';

import React, { useState, useEffect, useMemo, useRef, useCallback } from 'react';
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
import { submitGameResult } from '@/lib/api';

// ==========================================
// 1. DATASETS WITH 100% CLEAN UTF-8 VIETNAMESE
// ==========================================

interface VocabCard {
  id: string;
  en: string;
  vi: string;
  ipa: string;
  level: string;
}

const VOCAB_POOL: VocabCard[] = [
  { id: 'v1', en: 'curious', vi: 'tò mò; ham tìm hiểu', ipa: '/ˈkjʊəriəs/', level: 'A2' },
  { id: 'v2', en: 'journey', vi: 'hành trình; chuyến đi', ipa: '/ˈdʒɜːrni/', level: 'A2' },
  { id: 'v3', en: 'improve', vi: 'cải thiện; tiến bộ', ipa: '/ɪmˈpruːv/', level: 'A2' },
  { id: 'v4', en: 'achieve', vi: 'đạt được; hoàn thành', ipa: '/əˈtʃiːv/', level: 'A2' },
  { id: 'v5', en: 'habit', vi: 'thói quen hàng ngày', ipa: '/ˈhæbɪt/', level: 'A1' },
  { id: 'v6', en: 'careful', vi: 'cẩn thận; chu đáo', ipa: '/ˈkerfəl/', level: 'A1' },
  { id: 'v7', en: 'confident', vi: 'tự tin; tin tưởng', ipa: '/ˈkɒnfɪdənt/', level: 'A2' },
  { id: 'v8', en: 'discover', vi: 'khám phá; tìm tòi', ipa: '/dɪˈskʌvə(r)/', level: 'A2' },
  { id: 'v9', en: 'challenge', vi: 'thử thách; thách thức', ipa: '/ˈtʃælɪndʒ/', level: 'B1' },
  { id: 'v10', en: 'knowledge', vi: 'kiến thức; sự hiểu biết', ipa: '/ˈnɒlɪdʒ/', level: 'B1' },
  { id: 'v11', en: 'patient', vi: 'kiên nhẫn; bền lòng', ipa: '/ˈpeɪʃnt/', level: 'A2' },
  { id: 'v12', en: 'creative', vi: 'sáng tạo; mới lạ', ipa: '/kriˈeɪtɪv/', level: 'B1' },
  { id: 'v13', en: 'inspire', vi: 'truyền cảm hứng', ipa: '/ɪnˈspaɪər/', level: 'B1' },
  { id: 'v14', en: 'efficient', vi: 'hiệu quả; tối ưu', ipa: '/ɪˈfɪʃnt/', level: 'B2' },
  { id: 'v15', en: 'resilient', vi: 'kiên cường; phục hồi nhanh', ipa: '/rɪˈzɪliənt/', level: 'B2' },
  { id: 'v16', en: 'strategy', vi: 'chiến lược; kế hoạch', ipa: '/ˈstrætədʒi/', level: 'B1' },
];

interface ScrambleSentence {
  id: string;
  vi: string;
  tokens: string[];
  level: string;
}

const SCRAMBLE_POOL: ScrambleSentence[] = [
  { id: 's1', vi: 'Cô ấy chơi đàn piano rất giỏi.', tokens: ['She', 'plays', 'piano', 'very', 'well.'], level: 'A1' },
  { id: 's2', vi: 'Chúng tôi khám phá những từ mới mỗi ngày.', tokens: ['We', 'discover', 'new', 'words', 'every', 'day.'], level: 'A2' },
  { id: 's3', vi: 'Học một ngôn ngữ mới là một hành trình dài.', tokens: ['Learning', 'a', 'language', 'is', 'a', 'long', 'journey.'], level: 'A2' },
  { id: 's4', vi: 'Hãy cẩn thận khi bạn băng qua đường.', tokens: ['Be', 'careful', 'when', 'crossing', 'the', 'street.'], level: 'A1' },
  { id: 's5', vi: 'Luyện tập mỗi ngày giúp bạn tự tin hơn.', tokens: ['Daily', 'practice', 'makes', 'you', 'more', 'confident.'], level: 'B1' },
  { id: 's6', vi: 'Cô ấy đã nỗ lực hết mình để đạt được mục tiêu.', tokens: ['She', 'worked', 'hard', 'to', 'achieve', 'her', 'goal.'], level: 'B1' },
];

interface ClozeQuestion {
  id: string;
  prompt: string;
  choices: string[];
  correct: number;
  explanation: string;
  level: string;
}

const CLOZE_POOL: ClozeQuestion[] = [
  {
    id: 'c1',
    prompt: 'Reading books every single morning is a good _____ that everyone should maintain.',
    choices: ['habit', 'journey', 'careful', 'improve'],
    correct: 0,
    explanation: '"habit" (danh từ) nghĩa là thói quen. "A good habit" = một thói quen tốt.',
    level: 'A1',
  },
  {
    id: 'c2',
    prompt: 'She worked diligently day and night to _____ her lifelong ambition of becoming an architect.',
    choices: ['discover', 'achieve', 'patient', 'curious'],
    correct: 1,
    explanation: '"achieve" (động từ) đi với "ambition" hoặc "goal" nghĩa là đạt được, hoàn thành mục tiêu.',
    level: 'A2',
  },
  {
    id: 'c3',
    prompt: 'The young students are very _____ to know what happens next in the science experiment.',
    choices: ['curious', 'careful', 'efficient', 'patient'],
    correct: 0,
    explanation: 'Cụm "curious to know" nghĩa là tò mò, ham tìm hiểu muốn biết điều gì.',
    level: 'A2',
  },
  {
    id: 'c4',
    prompt: 'Regular practice will noticeably _____ your English communication skills over time.',
    choices: ['habit', 'improve', 'challenge', 'strategy'],
    correct: 1,
    explanation: '"improve" nghĩa là cải thiện, nâng cao kỹ năng (improve communication skills).',
    level: 'B1',
  },
  {
    id: 'c5',
    prompt: 'She was extremely _____ and waited two hours without complaining.',
    choices: ['resilient', 'patient', 'efficient', 'careful'],
    correct: 1,
    explanation: '"patient" nghĩa là kiên nhẫn, kiên trì chờ đợi.',
    level: 'A2',
  },
];

interface WordleChallenge {
  word: string;
  category: string;
  hint: string;
  meaning: string;
}

const WORDLE_POOL: WordleChallenge[] = [
  { word: 'WATER', category: 'Cuộc sống', hint: 'Chất lỏng trong suốt chiếm hơn 70% cơ thể người', meaning: 'Nước uống, nguồn sống' },
  { word: 'BRAIN', category: 'Khoa học', hint: 'Cơ quan chỉ huy suy nghĩ và lưu trữ trí nhớ', meaning: 'Bộ não, trí tuệ' },
  { word: 'HEART', category: 'Sinh học', hint: 'Cơ quan co bóp liên tục bơm máu nuôi cơ thể', meaning: 'Trái tim, tâm hồn' },
  { word: 'LEARN', category: 'Học tập', hint: 'Tiếp thu tri thức và kỹ năng mới mỗi ngày', meaning: 'Học hỏi, tiếp thu' },
  { word: 'LIGHT', category: 'Tự nhiên', hint: 'Ánh sáng giúp ta nhìn rõ mọi vật xung quanh', meaning: 'Ánh sáng, ban ngày' },
  { word: 'PEACE', category: 'Xã hội', hint: 'Trạng thái yên bình, không có chiến tranh', meaning: 'Hòa bình, thanh thản' },
  { word: 'SMART', category: 'Năng lực', hint: 'Nhanh trí, thông minh và khéo léo', meaning: 'Thông minh, sáng suốt' },
  { word: 'DREAM', category: 'Hy vọng', hint: 'Những ước mơ đẹp và khát vọng tương lai', meaning: 'Ước mơ, giấc mơ' },
];

// ==========================================
// 2. GAME CATALOG DEFINITIONS
// ==========================================

interface GameCatalogItem {
  id: string;
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
    title: 'Nối từ siêu tốc',
    englishTitle: 'Speed Word Match',
    desc: 'Ghép đôi từ vựng tiếng Anh với nghĩa tiếng Việt chuẩn xác trong thời gian 60 giây. Tích lũy chuỗi combo để nhân bội số điểm.',
    level: 'A1 - B1',
    timeLimit: '60s',
    xpReward: '+25 XP',
    icon: Zap,
  },
  {
    id: 'scramble',
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
    title: 'Đua tốc độ gõ',
    englishTitle: 'Typing Speedster',
    desc: 'Quan sát từ vựng, phiên âm IPA và nghĩa hiển thị, gõ lại thật nhanh chính xác để sang từ tiếp theo.',
    level: 'A1 - B2',
    timeLimit: '60s',
    xpReward: '+40 XP',
    icon: Keyboard,
  },
  {
    id: 'word-fill',
    title: 'Điền khuyết ngữ cảnh',
    englishTitle: 'Context Cloze Blitz',
    desc: 'Đọc câu hỏi ngữ cảnh và chọn 1 trong 4 phương án chính xác nhất để hoàn thiện câu văn chuẩn bản ngữ.',
    level: 'A2 - B2',
    timeLimit: '60s',
    xpReward: '+20 XP',
    icon: CheckCircle,
  },
  {
    id: 'audio-reflex',
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
    title: 'Lingo Wordle',
    englishTitle: '5-Letter Word Guess',
    desc: 'Thử tài suy luận từ vựng 5 ký tự với tối đa 6 lượt đoán và phản hồi màu sắc trực quan.',
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
  const [activeGame, setActiveGame] = useState<string | null>(null);

  // User Stats (cached locally + sync with auth)
  const [stats, setStats] = useState({
    gamesPlayed: 1,
    gamesWon: 1,
    totalXp: 138,
    rankTitle: 'Cao thủ',
  });

  useEffect(() => {
    try {
      const saved = localStorage.getItem('mezon_games_stats');
      if (saved) {
        setStats(JSON.parse(saved));
      }
    } catch {}
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

        {/* Tab Switcher */}
        {!activeGame && (
          <div className="games-tabs" role="tablist">
            <button
              className={`games-tab-btn ${activeTab === 'catalog' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('catalog')}
            >
              <Gamepad2 size={15} /> Kho trò chơi
            </button>
            <button
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
          gameId={activeGame}
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
                      onClick={() => setActiveGame(item.id)}
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
// 4. ACTIVE GAME ARENA CONTAINER
// ==========================================

function ActiveGameArena({
  gameId,
  onExit,
  onFinish,
}: {
  gameId: string;
  onExit: () => void;
  onFinish: (won: boolean, xp: number) => void;
}) {
  const [gameOver, setGameOver] = useState(false);
  const [finalScore, setFinalScore] = useState(0);
  const [finalAccuracy, setFinalAccuracy] = useState(100);
  const [earnedXp, setEarnedXp] = useState(25);

  const handleGameEnd = (score: number, accuracy: number, xp: number) => {
    setFinalScore(score);
    setFinalAccuracy(accuracy);
    setEarnedXp(xp);
    setGameOver(true);
    onFinish(score > 0, xp);

    // Call backend submit if available
    try {
      submitGameResult({
        attemptId: `attempt-${Date.now()}`,
        gameType: gameId,
        timeRemainingSeconds: 0,
        answers: [{ questionId: 'summary', answer: String(score) }],
      }).catch(() => {});
    } catch {}
  };

  if (gameOver) {
    return (
      <section className="quiz-results">
        <div className="quiz-result-icon">
          <Trophy size={36} />
        </div>
        <h2>Tuyệt vời! Hoàn thành trò chơi</h2>
        <p className="text-sm text-[#77786f] mt-1">
          Bạn đã thể hiện phản xạ rất xuất sắc trong phiên chơi này!
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
      {gameId === 'word-match' && <WordMatchEngine onEnd={handleGameEnd} onExit={onExit} />}
      {gameId === 'scramble' && <ScrambleEngine onEnd={handleGameEnd} onExit={onExit} />}
      {gameId === 'typing-race' && <TypingRaceEngine onEnd={handleGameEnd} onExit={onExit} />}
      {gameId === 'word-fill' && <ContextClozeEngine onEnd={handleGameEnd} onExit={onExit} />}
      {gameId === 'audio-reflex' && <AudioReflexEngine onEnd={handleGameEnd} onExit={onExit} />}
      {gameId === 'word-guess' && <WordleEngine onEnd={handleGameEnd} onExit={onExit} />}
    </div>
  );
}

// ==========================================
// 5. ENGINE 1: WORD MATCH (NỐI TỪ SIÊU TỐC)
// ==========================================

function WordMatchEngine({
  onEnd,
  onExit,
}: {
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

  // Init cards
  useEffect(() => {
    const subset = [...VOCAB_POOL].sort(() => 0.5 - Math.random()).slice(0, 8);
    const generated: { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[] = [];
    subset.forEach((v) => {
      generated.push({ id: `en-${v.id}`, text: v.en, pairId: v.id, type: 'en', isMatched: false });
      generated.push({ id: `vi-${v.id}`, text: v.vi, pairId: v.id, type: 'vi', isMatched: false });
    });
    setCards(generated.sort(() => 0.5 - Math.random()));
  }, []);

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
      // Match!
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
      // Mismatch
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
// 6. ENGINE 2: SENTENCE SCRAMBLE (XẾP CÂU)
// ==========================================

function ScrambleEngine({
  onEnd,
  onExit,
}: {
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [slottedTokens, setSlottedTokens] = useState<string[]>([]);
  const [availableTokens, setAvailableTokens] = useState<{ id: string; text: string }[]>([]);
  const [feedback, setFeedback] = useState<'correct' | 'wrong' | null>(null);

  const currentSentence = SCRAMBLE_POOL[currentIndex % SCRAMBLE_POOL.length];

  // Load sentence tokens
  useEffect(() => {
    if (!currentSentence) return;
    const shuffled = currentSentence.tokens
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

  // Token click: move available -> slotted
  const handleSlotToken = (item: { id: string; text: string }) => {
    setSlottedTokens((prev) => [...prev, item.text]);
    setAvailableTokens((prev) => prev.filter((t) => t.id !== item.id));
  };

  // Click slotted token: return back to pool
  const handleUnslotToken = (text: string, index: number) => {
    setSlottedTokens((prev) => prev.filter((_, i) => i !== index));
    setAvailableTokens((prev) => [...prev, { id: `${Date.now()}-${text}`, text }]);
  };

  // Check answer
  const handleVerify = () => {
    const isCorrect = slottedTokens.join(' ') === currentSentence.tokens.join(' ');
    if (isCorrect) {
      setFeedback('correct');
      setScore((s) => s + 30);
      setTimeout(() => {
        if (currentIndex + 1 >= SCRAMBLE_POOL.length) {
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
          Xếp câu chuẩn ({currentIndex + 1}/{SCRAMBLE_POOL.length})
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
          <p>Nghĩa tiếng Việt</p>
          <h3>"{currentSentence.vi}"</h3>
        </div>

        {/* Drop zone */}
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

        {/* Token Pool */}
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
// 7. ENGINE 3: TYPING RACE (ĐUA TỐC ĐỘ GÕ)
// ==========================================

function TypingRaceEngine({
  onEnd,
  onExit,
}: {
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [inputVal, setInputVal] = useState('');
  const [wordsDone, setWordsDone] = useState(0);

  const currentWord = VOCAB_POOL[currentIndex % VOCAB_POOL.length];

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      onEnd(score, 100, 40);
      return;
    }
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd]);

  // Handle input change
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
          Đua tốc độ gõ ({wordsDone} từ đã hoàn thành)
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
          <p className="typing-target-ipa">{currentWord.ipa}</p>
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
// 8. ENGINE 4: CONTEXT CLOZE (ĐIỀN KHUYẾT)
// ==========================================

function ContextClozeEngine({
  onEnd,
  onExit,
}: {
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedChoice, setSelectedChoice] = useState<number | null>(null);
  const [showExplanation, setShowExplanation] = useState(false);

  const currentQ = CLOZE_POOL[currentIndex % CLOZE_POOL.length];

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

    if (idx === currentQ.correct) {
      setScore((s) => s + 20);
    }
  };

  const handleNext = () => {
    if (currentIndex + 1 >= CLOZE_POOL.length) {
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
          Điền khuyết ngữ cảnh ({currentIndex + 1}/{CLOZE_POOL.length})
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
            const isRight = selectedChoice !== null && idx === currentQ.correct;
            return (
              <button
                key={idx}
                disabled={selectedChoice !== null}
                className={`${isRight ? 'is-right' : ''} ${isChosen && !isRight ? 'is-chosen' : ''}`}
                onClick={() => handleSelect(idx)}
              >
                <span className="quiz-option-key">
                  {String.fromCharCode(65 + idx)}
                </span>
                <span>{choice}</span>
                {isRight && <Check size={16} className="text-[#315d1d]" />}
              </button>
            );
          })}
        </div>

        {showExplanation && (
          <div className="quiz-explanation">
            <strong>Giải thích ngữ cảnh:</strong>
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
// 9. ENGINE 5: AUDIO REFLEX (PHẢN XẠ NGHE ÂM)
// ==========================================

function AudioReflexEngine({
  onEnd,
  onExit,
}: {
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);
  const [options, setOptions] = useState<string[]>([]);

  const currentWord = VOCAB_POOL[currentIndex % VOCAB_POOL.length];

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

  // Prepare 4 choices (1 correct + 3 distractor)
  useEffect(() => {
    if (!currentWord) return;
    const others = VOCAB_POOL.filter((v) => v.id !== currentWord.id)
      .sort(() => 0.5 - Math.random())
      .slice(0, 3)
      .map((v) => v.vi);
    const combined = [currentWord.vi, ...others].sort(() => 0.5 - Math.random());
    setOptions(combined);
    setSelectedIdx(null);
    playAudio(currentWord.en);
  }, [currentIndex, currentWord, playAudio]);

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

    if (options[idx] === currentWord.vi) {
      setScore((s) => s + 35);
      setTimeout(() => {
        if (currentIndex + 1 >= 8) {
          onEnd(score + 35, 100, 35);
        } else {
          setCurrentIndex((i) => i + 1);
        }
      }, 700);
    } else {
      setTimeout(() => {
        setCurrentIndex((i) => i + 1);
      }, 1000);
    }
  };

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> Thoát
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Phản xạ nghe âm ({currentIndex + 1}/8)
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

        {/* Big Audio Button */}
        <button
          className="audio-reflex-speaker-btn"
          onClick={() => playAudio(currentWord.en)}
          aria-label="Phát lại âm thanh"
        >
          <Volume2 size={36} />
        </button>

        <p className="text-xs text-[#8a897f] mb-6">Bấm vào biểu tượng loa để nghe lại âm thanh</p>

        <div className="quiz-options">
          {options.map((opt, idx) => {
            const isRight = selectedIdx !== null && opt === currentWord.vi;
            const isWrong = selectedIdx === idx && opt !== currentWord.vi;
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
// 10. ENGINE 6: WORDLE (ĐOÁN TỪ 5 KÝ TỰ)
// ==========================================

function WordleEngine({
  onEnd,
  onExit,
}: {
  onEnd: (score: number, accuracy: number, xp: number) => void;
  onExit: () => void;
}) {
  const [challenge] = useState<WordleChallenge>(
    () => WORDLE_POOL[Math.floor(Math.random() * WORDLE_POOL.length)]
  );
  const [guesses, setGuesses] = useState<string[]>([]);
  const [currentGuess, setCurrentGuess] = useState('');
  const [isGameOver, setIsGameOver] = useState(false);
  const [statusMsg, setStatusMsg] = useState('');

  const target = challenge.word;

  // Key press listener
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

  // Color evaluation for each row
  const getCellClass = (word: string, letterIndex: number) => {
    const char = word[letterIndex];
    if (target[letterIndex] === char) return 'correct';
    if (target.includes(char)) return 'present';
    return 'absent';
  };

  // Keyboard rows
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
        {/* Hint banner */}
        <div className="text-center mb-4">
          <p className="text-xs text-[#77786f]">Gợi ý: {challenge.hint}</p>
          <p className="text-xs font-semibold text-[#69734f] mt-1">Nghĩa: {challenge.meaning}</p>
          {statusMsg && (
            <p className="text-xs font-bold text-[#bd684d] mt-2 animate-bounce">{statusMsg}</p>
          )}
        </div>

        {/* Wordle 6x5 Grid */}
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

        {/* Virtual Keyboard */}
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
// 11. LEADERBOARD TAB (MEZON EDITORIAL DESIGN)
// ==========================================

function GamesLeaderboardTab() {
  const topThree = MOCK_LEADERBOARD.slice(0, 3);
  const remaining = MOCK_LEADERBOARD.slice(3);

  return (
    <section>
      {/* Podium Top 3 */}
      <div className="leaderboard-podium">
        {/* Rank 2 */}
        {topThree[1] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#2</span>
            <strong>{topThree[1].name}</strong>
            <span>{topThree[1].xp.toLocaleString('vi-VN')} XP</span>
          </div>
        )}

        {/* Rank 1 */}
        {topThree[0] && (
          <div className="leaderboard-podium-card rank-1">
            <span className="leaderboard-medal">🏆</span>
            <strong>{topThree[0].name}</strong>
            <span>{topThree[0].xp.toLocaleString('vi-VN')} XP</span>
          </div>
        )}

        {/* Rank 3 */}
        {topThree[2] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#3</span>
            <strong>{topThree[2].name}</strong>
            <span>{topThree[2].xp.toLocaleString('vi-VN')} XP</span>
          </div>
        )}
      </div>

      {/* Leaderboard Rows */}
      <div className="leaderboard-panel mt-6">
        <div className="leaderboard-panel-heading mb-4">
          <h2>Bảng vàng danh dự</h2>
          <span className="text-xs text-[#77786f]">Cập nhật theo tuần</span>
        </div>

        <div className="leaderboard-list">
          {remaining.map((entry) => (
            <div key={entry.user_id} className="leaderboard-row">
              <span className="leaderboard-rank">#{entry.rank}</span>
              <div className="leaderboard-avatar">
                {entry.name.slice(0, 1)}
              </div>
              <div className="leaderboard-name">
                <span>{entry.name}</span>
                <small>{entry.level}</small>
              </div>
              <span className="leaderboard-xp font-semibold">
                {entry.xp.toLocaleString('vi-VN')} XP
              </span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
