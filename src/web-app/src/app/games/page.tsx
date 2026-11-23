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
  VolumeX,
  CheckCircle,
  Shuffle,
  Keyboard,
  Check,
  X,
  Flame,
  HelpCircle,
} from 'lucide-react';
import { useAuth } from '@/components/auth/useAuth';
import { useLanguage } from '@/lib/language-context';
import { useSoundEffects } from '@/lib/sound-effects';
import GameTutorialModal, { isGameTutorialDismissed } from '@/components/GameTutorialModal';
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
  descEn: string;
  level: string;
  timeLimit: string;
  timeLimitEn: string;
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
    descEn: 'Match English vocabulary with their Vietnamese meanings accurately within 60 seconds. Powered by 500 CEFR words from Database.',
    level: 'A1 - B1',
    timeLimit: '60s',
    timeLimitEn: '60s',
    xpReward: '+25 XP',
    icon: Zap,
  },
  {
    id: 'scramble',
    apiKey: 'sentence_scramble',
    title: 'Xếp câu chuẩn',
    englishTitle: 'Sentence Scramble',
    desc: 'Sắp xếp các mảnh từ bị xáo trộn thành câu văn hoàn chỉnh đúng ngữ pháp và cấu trúc ngữ nghĩa.',
    descEn: 'Arrange scrambled word chips into a grammatically correct English sentence from the Vietnamese meaning hint.',
    level: 'A1 - B2',
    timeLimit: '60s',
    timeLimitEn: '60s',
    xpReward: '+30 XP',
    icon: Shuffle,
  },
  {
    id: 'typing-race',
    apiKey: 'typing_race',
    title: 'Đua tốc độ gõ',
    englishTitle: 'Typing Speedster',
    desc: 'Quan sát từ vựng, phiên âm IPA và nghĩa hiển thị từ Database, gõ lại thật nhanh chính xác để sang từ tiếp theo.',
    descEn: 'Read the vocabulary word, IPA transcription, and meaning from Database, then type it accurately as fast as you can.',
    level: 'A1 - B2',
    timeLimit: '60s',
    timeLimitEn: '60s',
    xpReward: '+40 XP',
    icon: Keyboard,
  },
  {
    id: 'word-fill',
    apiKey: 'fill_blitz',
    title: 'Điền khuyết ngữ cảnh',
    englishTitle: 'Context Cloze Blitz',
    desc: 'Đọc câu hỏi ngữ cảnh từ kho 300 câu hỏi và chọn 1 trong 4 phương án chính xác nhất để hoàn thiện câu.',
    descEn: 'Read context questions from the 300-question bank and pick the best answer among 4 choices to complete each prompt.',
    level: 'A2 - B2',
    timeLimit: '60s',
    timeLimitEn: '60s',
    xpReward: '+20 XP',
    icon: CheckCircle,
  },
  {
    id: 'audio-reflex',
    apiKey: 'sound_reflex',
    title: 'Phản xạ nghe âm',
    englishTitle: 'Audio Ear Reflex',
    desc: 'Lắng nghe máy phát âm từ vựng bản xứ và bấm ngay vào nghĩa tiếng Việt tương ứng trong 4 phương án.',
    descEn: 'Listen to native English pronunciation and tap the matching Vietnamese meaning among 4 choices.',
    level: 'A1 - B1',
    timeLimit: '60s',
    timeLimitEn: '60s',
    xpReward: '+35 XP',
    icon: Volume2,
  },
  {
    id: 'word-guess',
    apiKey: 'lingo_wordle',
    title: 'Lingo Wordle',
    englishTitle: '5-Letter Word Guess',
    desc: 'Thử tài suy luận từ vựng 5 ký tự ngẫu nhiên từ Database với tối đa 6 lượt đoán và phản hồi màu sắc trực quan.',
    descEn: 'Deduce a random 5-letter English word from Database within 6 tries using color-coded server feedback.',
    level: 'B1 - B2',
    timeLimit: 'Không giới hạn',
    timeLimitEn: 'Unlimited',
    xpReward: '+50 XP',
    icon: Gamepad2,
  },
];

// ==========================================
// 2. MAIN COMPONENT
// ==========================================

export default function GameCenterPage() {
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const { translate: t, language } = useLanguage();
  const { sfxEnabled, toggleSound, playClick } = useSoundEffects();
  const [activeTab, setActiveTab] = useState<'catalog' | 'leaderboard'>('catalog');
  const [selectedLevel, setSelectedLevel] = useState<string>('all');
  const [activeGame, setActiveGame] = useState<GameCatalogItem | null>(null);
  const [tutorialGame, setTutorialGame] = useState<GameCatalogItem | null>(null);
  const [tutorialLaunchMode, setTutorialLaunchMode] = useState<'launch' | 'inspect'>('launch');

  const handlePlayGame = useCallback((item: GameCatalogItem) => {
    playClick();
    if (!isGameTutorialDismissed(item.apiKey)) {
      setTutorialLaunchMode('launch');
      setTutorialGame(item);
      return;
    }
    setActiveGame(item);
  }, [playClick]);

  const handleOpenGameRules = useCallback((item: GameCatalogItem) => {
    playClick();
    setTutorialLaunchMode('inspect');
    setTutorialGame(item);
  }, [playClick]);

  // Player Stats (Real stats only, initialized to 0)
  const [stats, setStats] = useState({
    gamesPlayed: 0,
    gamesWon: 0,
    totalXp: 0,
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
          });
        }
      })
      .catch(() => { });
  }, [isAuthenticated, user?.id]);

  const rankTitle =
    stats.gamesPlayed > 10
      ? t('Đại cao thủ', 'Grandmaster')
      : stats.gamesPlayed > 3
        ? t('Cao thủ', 'Master')
        : t('Tập sự', 'Novice');

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
          <h1>{t('Đấu trường trò chơi từ vựng', 'Vocabulary Game Arena')}</h1>
          <p className="games-subtitle">
            {t(
              'Rèn luyện từ vựng, phản xạ nghe và ngữ pháp tiếng Anh thông qua 6 mini-game tương tác. Tích lũy điểm số, thăng hạng Leaderboard và gom thưởng XP mỗi ngày!',
              'Sharpen your vocabulary, listening reflexes, and English grammar across 6 interactive mini-games. Earn high scores, climb the Leaderboard, and collect XP every day!'
            )}
          </p>
        </div>

        {/* Tab Switcher + SFX Toggle */}
        {!activeGame && (
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              className="sfx-toggle-btn"
              onClick={toggleSound}
              aria-pressed={sfxEnabled}
              aria-label={sfxEnabled ? t('Tắt hiệu ứng âm thanh', 'Mute sound effects') : t('Bật hiệu ứng âm thanh', 'Enable sound effects')}
            >
              {sfxEnabled ? <Volume2 size={15} /> : <VolumeX size={15} />}
              <span>{sfxEnabled ? t('Âm thanh Bật', 'SFX On') : t('Âm thanh Tắt', 'SFX Off')}</span>
            </button>
            <div className="games-tabs" role="tablist">
              <button
                type="button"
                className={`games-tab-btn ${activeTab === 'catalog' ? 'is-active' : ''}`}
                onClick={() => setActiveTab('catalog')}
              >
                <Gamepad2 size={15} /> {t('Kho trò chơi', 'Game Catalog')}
              </button>
              <button
                type="button"
                className={`games-tab-btn ${activeTab === 'leaderboard' ? 'is-active' : ''}`}
                onClick={() => setActiveTab('leaderboard')}
              >
                <Trophy size={15} /> {t('Bảng xếp hạng', 'Leaderboard')}
              </button>
            </div>
          </div>
        )}
      </header>

      {/* Auth prompt banner for guest visitors */}
      {!authLoading && !isAuthenticated && !activeGame && (
        <div className="mb-6 p-4 rounded-2xl bg-amber-50 border border-amber-200 flex flex-wrap items-center justify-between gap-3 text-xs sm:text-sm text-amber-900">
          <span>
            <strong>{t('Yêu cầu đăng nhập:', 'Sign-in required:')}</strong>{' '}
            {t(
              'Vui lòng đăng nhập Mezon SSO để tải bộ câu hỏi từ Database, lưu kỷ lục và nhận thưởng XP trong Game Center.',
              'Please sign in with Mezon SSO to load live questions from Database, save records, and earn XP in Game Center.'
            )}
          </span>
          <Link
            href="/auth"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[var(--olive,#69734f)] text-white font-semibold hover:opacity-90 transition-all"
          >
            {t('Đăng nhập ngay', 'Sign in now')} <ArrowRight size={14} />
          </Link>
        </div>
      )}

      {/* 2. Personal Stats Overview */}
      {!activeGame && (
        <section className="games-stats-grid" aria-label={t('Thống kê người chơi', 'Player statistics')}>
          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Gamepad2 size={20} />
            </div>
            <div className="games-stat-info">
              <small>{t('Tổng trận đã chơi', 'Games played')}</small>
              <strong>{stats.gamesPlayed}</strong>
              <span>{t('Đang hoạt động', 'Active')}</span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Trophy size={20} />
            </div>
            <div className="games-stat-info">
              <small>{t('Tổng trận thắng', 'Games won')}</small>
              <strong>{stats.gamesWon}</strong>
              <span>
                {t('Tỉ lệ', 'Win rate')} {Math.round((stats.gamesWon / Math.max(stats.gamesPlayed, 1)) * 100)}%
              </span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Zap size={20} />
            </div>
            <div className="games-stat-info">
              <small>{t('Tổng XP thu về', 'Total XP earned')}</small>
              <strong>+{stats.totalXp} XP</strong>
              <span>{t('Tích lũy tài khoản', 'Account total')}</span>
            </div>
          </div>

          <div className="games-stat-card">
            <div className="games-stat-icon">
              <Flame size={20} />
            </div>
            <div className="games-stat-info">
              <small>{t('Hạng kỷ lục', 'Rank tier')}</small>
              <strong>{rankTitle}</strong>
              <span>{t('Huy hiệu danh dự', 'Honor badge')}</span>
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
                {t('Mọi cấp độ', 'All levels')}
              </button>
              {['A1', 'A2', 'B1', 'B2'].map((lvl) => (
                <button
                  key={lvl}
                  className={`games-level-btn ${selectedLevel === lvl ? 'is-active' : ''}`}
                  onClick={() => setSelectedLevel(lvl)}
                >
                  {t(`Cấp độ ${lvl}`, `Level ${lvl}`)}
                </button>
              ))}
            </div>
            <span className="text-xs text-[#77786f]">
              {t(`Hiển thị ${filteredCatalog.length} trò chơi`, `Showing ${filteredCatalog.length} games`)}
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
                        <Clock size={13} /> {language === 'vi' ? item.timeLimit : item.timeLimitEn}
                      </span>
                    </div>

                    <div className="game-catalog-body">
                      <div className="game-catalog-icon-title">
                        <div className="game-catalog-icon">
                          <IconComp size={22} />
                        </div>
                        <div>
                          <h3>{language === 'vi' ? item.title : item.englishTitle}</h3>
                          <p>{language === 'vi' ? item.englishTitle : item.title}</p>
                        </div>
                      </div>
                      <p className="game-catalog-desc">{language === 'vi' ? item.desc : item.descEn}</p>
                    </div>
                  </div>

                  <div className="game-catalog-footer">
                    <span className="game-catalog-xp">
                      <Zap size={14} /> {item.xpReward}
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        className="game-catalog-rules-btn"
                        onClick={() => handleOpenGameRules(item)}
                        aria-label={`${t('Luật chơi', 'Rules')} ${language === 'vi' ? item.title : item.englishTitle}`}
                        title={t('Xem hướng dẫn luật chơi', 'View how to play')}
                      >
                        <HelpCircle size={14} /> {t('Luật chơi', 'Rules')}
                      </button>
                      <button
                        type="button"
                        className="game-catalog-play-btn"
                        onClick={() => handlePlayGame(item)}
                      >
                        {t('Chơi ngay', 'Play now')} <ArrowRight size={14} />
                      </button>
                    </div>
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

      {tutorialGame && (
        <GameTutorialModal
          gameApiKey={tutorialGame.apiKey}
          isOpen={Boolean(tutorialGame)}
          onClose={() => setTutorialGame(null)}
          onStart={() => {
            const target = tutorialGame;
            setTutorialGame(null);
            setActiveGame(target);
          }}
          actionLabel={
            tutorialLaunchMode === 'inspect'
              ? t('Chơi ngay', 'Play now')
              : t('Bắt đầu chơi ngay', 'Start playing now')
          }
        />
      )}
    </main>
  );
}

// ==========================================
// 3. ACTIVE GAME ARENA CONTAINER (DB SYNCED)
// ==========================================

function shuffleWithSeed<T>(items: T[], seedStr: string): T[] {
  const copy = [...items];
  let h = 2166136261;
  for (let i = 0; i < seedStr.length; i += 1) {
    h ^= seedStr.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  for (let i = copy.length - 1; i > 0; i -= 1) {
    h ^= h << 13;
    h ^= h >>> 17;
    h ^= h << 5;
    const j = (h >>> 0) % (i + 1);
    [copy[i], copy[j]] = [copy[j], copy[i]];
  }
  return copy;
}

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
  const { translate: t } = useLanguage();
  const { sfxEnabled, toggleSound, playVictory } = useSoundEffects();
  const [gameOver, setGameOver] = useState(false);
  const [showRulesModal, setShowRulesModal] = useState(false);
  const [replayKey, setReplayKey] = useState(0);
  const [finalScore, setFinalScore] = useState(0);
  const [finalAccuracy, setFinalAccuracy] = useState(100);
  const [earnedXp, setEarnedXp] = useState(25);

  const requestKey = `${game.apiKey}:${level}:${replayKey}`;
  const [fetchState, setFetchState] = useState<{
    key: string;
    data: GameDataPayload | null;
    error: string | null;
  }>({ key: '', data: null, error: null });

  // Fetch live game data from Database on start / replay
  useEffect(() => {
    let isMounted = true;

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
          setFetchState({ key: requestKey, data: d, error: null });
        } else {
          setFetchState({
            key: requestKey,
            data: null,
            error: 'Chưa có dữ liệu cho trò chơi này. Vui lòng thử lại sau.',
          });
        }
      })
      .catch((err: Error & { status?: number }) => {
        if (isMounted) {
          console.warn('Could not fetch DB game data:', err);
          setFetchState({
            key: requestKey,
            data: null,
            error:
              err?.status === 401
                ? 'Bạn cần đăng nhập tài khoản Mezon để chơi và lưu điểm.'
                : 'Chưa có dữ liệu cho trò chơi này. Vui lòng thử lại sau.',
          });
        }
      });

    return () => {
      isMounted = false;
    };
  }, [game.apiKey, level, requestKey]);

  const loading = fetchState.key !== requestKey;
  const dbData = loading ? null : fetchState.data;
  const loadError = loading ? null : fetchState.error;

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
    playVictory();

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
        <p className="text-sm font-semibold text-[#55574f]">
          {t('Đang kết nối Database và tải câu hỏi...', 'Connecting to Database and loading questions...')}
        </p>
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="game-stage-box text-center py-14">
        <X size={36} className="text-[#bd684d] mx-auto mb-3" />
        <h3 className="text-base font-bold text-[#252820] mb-2">{loadError}</h3>
        <p className="text-xs text-[#77786f] mb-6">
          {t(
            'Hệ thống không tìm thấy gói câu hỏi phù hợp từ máy chủ hoặc bạn cần đăng nhập.',
            'No matching question pack was found on the server, or you need to sign in.'
          )}
        </p>
        <button className="quiz-secondary inline-flex items-center gap-2" onClick={onExit}>
          <ArrowLeft size={15} /> {t('Quay lại kho trò chơi', 'Back to Game Catalog')}
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
        <h2>{t('Tuyệt vời! Hoàn thành trò chơi', 'Great job! Game Completed')}</h2>
        <p className="text-sm text-[#77786f] mt-1">
          {t(
            'Dữ liệu đã được đồng bộ với máy chủ và ghi nhận điểm số của bạn.',
            'Your score and XP have been verified and saved on the server.'
          )}
        </p>

        <div className="quiz-result-stats">
          <div>
            <span>{t('ĐIỂM SỐ', 'SCORE')}</span>
            <strong>{finalScore} pts</strong>
          </div>
          <div>
            <span>{t('XP NHẬN ĐƯỢC', 'XP EARNED')}</span>
            <strong className="text-[#69734f]">+{earnedXp} XP</strong>
          </div>
          <div>
            <span>{t('ĐỘ CHÍNH XÁC', 'ACCURACY')}</span>
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
            <RotateCcw size={15} /> {t('Chơi lại lần nữa', 'Play Again')}
          </button>
          <button className="quiz-secondary" onClick={onExit}>
            <ArrowLeft size={15} /> {t('Về kho trò chơi', 'Back to Catalog')}
          </button>
        </div>
      </section>
    );
  }

  return (
    <div className="game-arena-wrapper">
      <div className="game-arena-utility-strip">
        <button
          type="button"
          className="game-catalog-rules-btn"
          onClick={() => setShowRulesModal(true)}
        >
          <HelpCircle size={14} /> {t('Luật chơi', 'How to play')}
        </button>
        <button
          type="button"
          className="sfx-toggle-btn"
          onClick={toggleSound}
          aria-pressed={sfxEnabled}
          aria-label={sfxEnabled ? t('Tắt hiệu ứng âm thanh', 'Mute sound effects') : t('Bật hiệu ứng âm thanh', 'Enable sound effects')}
        >
          {sfxEnabled ? <Volume2 size={15} /> : <VolumeX size={15} />}
          <span>{sfxEnabled ? t('Âm thanh Bật', 'SFX On') : t('Âm thanh Tắt', 'SFX Off')}</span>
        </button>
      </div>
      {game.id === 'word-match' && (
        <WordMatchEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'scramble' && (
        <ScrambleEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'typing-race' && (
        <TypingRaceEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'word-fill' && (
        <ContextClozeEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'audio-reflex' && (
        <AudioReflexEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      {game.id === 'word-guess' && (
        <WordleEngine key={dbData?.attemptId ?? requestKey} dbData={dbData} onEnd={handleGameEnd} onExit={onExit} />
      )}
      <GameTutorialModal
        gameApiKey={game.apiKey}
        isOpen={showRulesModal}
        onClose={() => setShowRulesModal(false)}
        onStart={() => setShowRulesModal(false)}
        actionLabel={t('Tiếp tục chơi', 'Resume game')}
      />
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
  const { translate: t } = useLanguage();
  const { playCorrect, playWrong, playClick, playTick } = useSoundEffects();
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [combo, setCombo] = useState(0);
  const [cards, setCards] = useState<
    { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[]
  >(() => {
    const rawItems = dbData?.items || [];
    const generated: { id: string; text: string; pairId: string; type: 'en' | 'vi'; isMatched: boolean }[] = [];
    rawItems.slice(0, 8).forEach((item) => {
      generated.push({ id: `en-${item.id}`, text: item.targetText, pairId: item.id, type: 'en', isMatched: false });
      generated.push({ id: `vi-${item.id}`, text: item.translation, pairId: item.id, type: 'vi', isMatched: false });
    });
    return shuffleWithSeed(generated, dbData?.attemptId || 'word-match');
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mismatchIds, setMismatchIds] = useState<string[]>([]);
  const matchedAnswersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const endedRef = useRef(false);

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.min(100, Math.round((matchedAnswersRef.current.length / Math.max(cards.length / 2, 1)) * 100)), 25, matchedAnswersRef.current, 0);
      }
      return;
    }
    if (timeLeft <= 5) playTick(timeLeft <= 2);
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, cards.length, onEnd, playTick]);

  // Click card handler
  const handleCardClick = (cardId: string) => {
    const card = cards.find((c) => c.id === cardId);
    if (!card || card.isMatched || mismatchIds.length > 0) return;

    if (!selectedId) {
      playClick();
      setSelectedId(cardId);
      return;
    }

    if (selectedId === cardId) {
      playClick();
      setSelectedId(null);
      return;
    }

    const prevCard = cards.find((c) => c.id === selectedId);
    if (!prevCard) {
      playClick();
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
      playCorrect(newCombo);
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
      playWrong();
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
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">{t('Nối từ siêu tốc', 'Speed Word Match')}</div>
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
                className={`word-match-card ${isSel ? 'is-selected' : ''} ${c.isMatched ? 'is-matched' : ''
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
  const { translate: t } = useLanguage();
  const { playCorrect, playWrong, playClick, playTick } = useSoundEffects();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const sentences = useMemo(() => {
    const fromDb = dbData?.questions || [];
    return fromDb.map((q) => ({
      id: q.id,
      prompt: q.prompt,
      tokens: q.tokens || (q.answer || q.prompt).split(' '),
      answer: q.answer || q.prompt,
    }));
  }, [dbData]);

  const buildTokensForSentence = useCallback(
    (sentence: (typeof sentences)[number] | undefined, seedSuffix: string) => {
      if (!sentence) return [];
      const mapped = sentence.tokens.map((tok, idx) => ({ id: `${idx}-${tok}`, text: tok }));
      return shuffleWithSeed(mapped, `${dbData?.attemptId || 'scramble'}-${sentence.id}-${seedSuffix}`);
    },
    [dbData?.attemptId]
  );

  const [slottedTokens, setSlottedTokens] = useState<string[]>([]);
  const [availableTokens, setAvailableTokens] = useState<{ id: string; text: string }[]>(() =>
    buildTokensForSentence(sentences[0], '0')
  );
  const [feedback, setFeedback] = useState<'correct' | 'wrong' | null>(null);
  const [wrongAttempts, setWrongAttempts] = useState(0);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const endedRef = useRef(false);

  const currentSentence = sentences[currentIndex % (sentences.length || 1)];

  const moveToSentence = useCallback(
    (nextIdx: number) => {
      const nextSentence = sentences[nextIdx % (sentences.length || 1)];
      setCurrentIndex(nextIdx);
      setAvailableTokens(buildTokensForSentence(nextSentence, String(nextIdx)));
      setSlottedTokens([]);
      setFeedback(null);
      setWrongAttempts(0);
    },
    [sentences, buildTokensForSentence]
  );

  // Timer countdown
  useEffect(() => {
    if (timeLeft <= 0) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.round((answersRef.current.length / Math.max(sentences.length, 1)) * 100), 30, answersRef.current, 0);
      }
      return;
    }
    if (timeLeft <= 5) playTick(timeLeft <= 2);
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, sentences.length, onEnd, playTick]);

  const handleSlotToken = (item: { id: string; text: string }) => {
    playClick();
    setFeedback(null);
    setSlottedTokens((prev) => [...prev, item.text]);
    setAvailableTokens((prev) => prev.filter((t) => t.id !== item.id));
  };

  const handleUnslotToken = (text: string, index: number) => {
    playClick();
    setFeedback(null);
    setSlottedTokens((prev) => prev.filter((_, i) => i !== index));
    setAvailableTokens((prev) => [...prev, { id: `${Date.now()}-${text}`, text }]);
  };

  const handleResetSentence = () => {
    if (!currentSentence) return;
    playClick();
    setAvailableTokens(
      [...currentSentence.tokens].map((t, idx) => ({ id: `${Date.now()}-${idx}-${t}`, text: t })).sort(() => 0.5 - Math.random())
    );
    setSlottedTokens([]);
    setFeedback(null);
  };

  const handleSkipSentence = () => {
    playClick();
    if (currentIndex + 1 >= sentences.length) {
      if (!endedRef.current) {
        endedRef.current = true;
        onEnd(score, Math.round((answersRef.current.length / Math.max(sentences.length, 1)) * 100), 30, answersRef.current, timeLeft);
      }
    } else {
      moveToSentence(currentIndex + 1);
    }
  };

  const handleVerify = () => {
    if (!currentSentence) return;
    const rawUserAns = slottedTokens.join(' ').trim();
    const userAns = rawUserAns.toLowerCase();
    const correctAns = currentSentence.answer.trim().toLowerCase();
    const isCorrect = userAns === correctAns;

    if (isCorrect) {
      playCorrect(1);
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
          moveToSentence(currentIndex + 1);
        }
      }, 750);
    } else {
      playWrong();
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
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          {t('Xếp câu chuẩn', 'Sentence Scramble')} ({currentIndex + 1}/{sentences.length})
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
          <p>
            {t(
              'Gợi ý nghĩa tiếng Việt — Sắp xếp thành câu tiếng Anh đúng',
              'Vietnamese meaning hint — Arrange into the right English sentence'
            )}
          </p>
          <h3>“{currentSentence.prompt}”</h3>
        </div>

        <div
          className={`sentence-drop-area ${feedback === 'correct'
            ? 'is-correct'
            : feedback === 'wrong'
              ? 'is-wrong'
              : ''
            }`}
        >
          {slottedTokens.length === 0 ? (
            <span className="text-xs text-[#8a897f]">
              {t('Bấm các từ bên dưới để xếp vào đây...', 'Tap the words below to build your sentence...')}
            </span>
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
            {t('Chính xác! +30 điểm — Đang chuyển sang câu tiếp theo...', 'Correct! +30 pts — Loading next sentence...')}
          </div>
        )}
        {feedback === 'wrong' && (
          <div className="sentence-feedback-msg is-wrong">
            {availableTokens.length > 0
              ? t(
                `Bạn chưa xếp hết các từ (còn ${availableTokens.length} từ bên dưới).`,
                `Please place all remaining words (${availableTokens.length} left below).`
              )
              : wrongAttempts >= 2
                ? t(
                  `Thứ tự chưa khớp đáp án chuẩn — Gợi ý: câu bắt đầu bằng "${firstWord}" và kết thúc bằng "${lastWord}".`,
                  `Word order does not match yet — Hint: starts with "${firstWord}" and ends with "${lastWord}".`
                )
                : t(
                  'Thứ tự từ chưa đúng đáp án chuẩn. Bấm vào từ ở khung trên để gỡ ra và xếp lại!',
                  'Word order is not quite right. Tap any placed word above to move it back!'
                )}
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
            <RotateCcw size={15} /> {t('Xếp lại', 'Reset')}
          </button>
          <button
            type="button"
            className="quiz-primary"
            style={{ marginLeft: 0 }}
            disabled={slottedTokens.length === 0}
            onClick={handleVerify}
          >
            <Check size={16} /> {t('Kiểm tra câu', 'Check sentence')}
          </button>
          <button
            type="button"
            className="quiz-secondary"
            onClick={handleSkipSentence}
          >
            {t('Bỏ qua', 'Skip')} <ArrowRight size={15} />
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
  const { translate: t } = useLanguage();
  const { playCorrect, playTick } = useSoundEffects();
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
    if (timeLeft <= 5) playTick(timeLeft <= 2);
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, wordList.length, onEnd, playTick]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!currentWord) return;
    const val = e.target.value;
    setInputVal(val);

    if (val.trim().toLowerCase() === currentWord.en.toLowerCase()) {
      playCorrect(Math.min(wordsDone + 1, 6));
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
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          {t(`Đua tốc độ gõ (${wordsDone} từ hoàn thành)`, `Typing Speedster (${wordsDone} completed)`)}
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
          <p className="typing-target-meaning">{t('Nghĩa', 'Meaning')}: {currentWord.vi}</p>
        </div>

        <input
          type="text"
          autoFocus
          className="typing-input-field"
          placeholder={t('Gõ lại chính xác từ vựng trên...', 'Type the exact word shown above...')}
          value={inputVal}
          onChange={handleChange}
        />
        <p className="text-center text-xs text-[#77786f] mt-3">
          {t('Hệ thống sẽ tự động chuyển từ tiếp theo ngay khi bạn gõ đúng!', 'Automatically advances to the next word as soon as you type it correctly!')}
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
  const { translate: t } = useLanguage();
  const { playCorrect, playWrong, playTick } = useSoundEffects();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedChoice, setSelectedChoice] = useState<number | null>(null);
  const [showExplanation, setShowExplanation] = useState(false);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const correctCountRef = useRef(0);
  const endedRef = useRef(false);

  const questions = useMemo(() => {
    type RawChoice = string | { targetText?: string; translation?: string };
    const raw = dbData?.questions || [];
    return raw.map((q) => {
      let choicesArr: string[] = [];
      const rawChoices = (q as { choices?: RawChoice[] | string }).choices;
      if (Array.isArray(rawChoices)) {
        choicesArr = rawChoices.map((c: RawChoice) =>
          typeof c === 'string' ? c : c.targetText || c.translation || String(c)
        );
      } else if (typeof rawChoices === 'string') {
        try {
          const parsed = JSON.parse(rawChoices);
          choicesArr = Array.isArray(parsed) ? parsed.map(String) : rawChoices.trim().split(/\s+/);
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
    if (timeLeft <= 5) playTick(timeLeft <= 2);
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd, playTick]);

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
      playCorrect(1);
    } else {
      playWrong();
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
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          {t('Điền khuyết ngữ cảnh', 'Context Cloze Blitz')} ({currentIndex + 1}/{questions.length})
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
          <span>{t('Chọn đáp án đúng nhất để hoàn thiện câu', 'Choose the best answer to complete the question')}</span>
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
            <strong>
              {currentQ.correctAnswer
                ? t(`Đáp án đúng: ${currentQ.correctAnswer}`, `Correct answer: ${currentQ.correctAnswer}`)
                : t('Đã ghi nhận đáp án:', 'Answer recorded:')}
            </strong>
            <p>{t('Điểm số và XP cuối cùng sẽ được máy chủ xác thực khi kết thúc lượt chơi.', 'Final score and XP will be verified by the server at the end of the session.')}</p>
          </div>
        )}

        {selectedChoice !== null && (
          <div className="flex justify-end mt-5">
            <button className="quiz-primary" onClick={handleNext}>
              {t('Câu tiếp theo', 'Next question')} <ArrowRight size={15} />
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
  const { translate: t } = useLanguage();
  const { playCorrect, playWrong, playTick } = useSoundEffects();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(60);
  const [score, setScore] = useState(0);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);
  const answersRef = useRef<{ questionId: string; answer: string }[]>([]);
  const correctCountRef = useRef(0);
  const endedRef = useRef(false);

  const questions = useMemo(() => {
    type RawAudioQuestion = {
      id: string;
      audioText?: string;
      choices?: unknown;
    };
    const raw = (dbData?.questions || []) as RawAudioQuestion[];
    return raw.map((q) => {
      let rawChoices: unknown = q.choices || [];
      if (typeof rawChoices === 'string') {
        const rawStr = rawChoices;
        try {
          rawChoices = JSON.parse(rawStr);
        } catch {
          rawChoices = rawStr.split(',');
        }
      }
      const choicesList: { label: string; targetText: string }[] = Array.isArray(rawChoices)
        ? rawChoices.map((c: unknown) => {
          if (typeof c === 'object' && c !== null) {
            const obj = c as { translation?: string; targetText?: string };
            return {
              label: obj.translation || obj.targetText || '',
              targetText: obj.targetText || obj.translation || '',
            };
          }
          return { label: String(c), targetText: String(c) };
        })
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
    if (timeLeft <= 5) playTick(timeLeft <= 2);
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, score, onEnd, playTick]);

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
      playCorrect(1);
    } else {
      playWrong();
    }

    setTimeout(() => {
      if (currentIndex + 1 >= questions.length) {
        if (!endedRef.current) {
          endedRef.current = true;
          const acc = Math.round((correctCountRef.current / Math.max(questions.length, 1)) * 100);
          onEnd(nextScore, acc, 35, answersRef.current, timeLeft);
        }
      } else {
        setSelectedIdx(null);
        setCurrentIndex((i) => i + 1);
      }
    }, 700);
  };

  if (!currentQ) return null;

  return (
    <div>
      <div className="game-arena-bar">
        <button onClick={onExit} className="game-arena-back">
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          {t('Phản xạ nghe âm', 'Audio Ear Reflex')} ({currentIndex + 1}/{questions.length})
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
          {t('Lắng nghe phát âm và chọn nghĩa tiếng Việt chuẩn xác', 'Listen to the pronunciation and select the matching meaning')}
        </p>

        <button
          className="audio-reflex-speaker-btn"
          onClick={() => playAudio(currentQ.audioText)}
          aria-label={t('Phát lại âm thanh', 'Replay audio')}
        >
          <Volume2 size={36} />
        </button>

        <p className="text-xs text-[#8a897f] mb-6">
          {t('Bấm vào biểu tượng loa để nghe lại âm thanh', 'Tap the speaker icon to hear the word again')}
        </p>

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
  const { translate: t } = useLanguage();
  const { playCorrect, playWrong, playClick } = useSoundEffects();
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
          playWrong();
          setStatusMsg(t('Vui lòng nhập đủ 5 chữ cái', 'Please enter all 5 letters'));
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
            playCorrect(3);
            setIsGameOver(true);
            setStatusMsg(t('Chúc mừng bạn đã đoán chính xác!', 'Congratulations! You guessed the word!'));
            setTimeout(() => {
              onEnd(100, 100, 50, answersBuffer.current);
            }, 1200);
          } else if (res.isGameOver || nextGuesses.length >= 6) {
            playWrong();
            setIsGameOver(true);
            setStatusMsg(t('Hết lượt đoán! Trò chơi kết thúc.', 'Out of guesses! Game over.'));
            setTimeout(() => {
              onEnd(20, 40, 15, answersBuffer.current);
            }, 1500);
          } else if (res.evaluations.includes('correct')) {
            playCorrect(1);
          } else {
            playWrong();
          }
        } catch (err: unknown) {
          playWrong();
          const msg = err instanceof Error ? err.message : '';
          setStatusMsg(msg || t('Không thể kết nối máy chủ', 'Could not connect to server'));
          setTimeout(() => setStatusMsg(''), 2000);
        } finally {
          setIsSubmitting(false);
        }
      } else if (key === 'BACK' || key === '⌫') {
        playClick();
        setCurrentGuess((prev) => prev.slice(0, -1));
      } else if (/^[A-Z]$/.test(key) && currentGuess.length < 5) {
        playClick();
        setCurrentGuess((prev) => prev + key);
      }
    },
    [attemptId, currentGuess, evaluations, guesses, isGameOver, isSubmitting, onEnd, playClick, playCorrect, playWrong, t]
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
          <ArrowLeft size={15} /> {t('Thoát', 'Exit')}
        </button>
        <div className="font-semibold text-sm text-[#252820]">
          Lingo Wordle ({guesses.length}/6 {t('lượt đoán', 'guesses')})
        </div>
        <div className="game-arena-metrics">
          <span className="game-arena-metric-pill">
            <HelpCircle size={14} /> {t('Chủ đề', 'Topic')}: {category}
          </span>
        </div>
      </div>

      <div className="game-stage-box">
        <div className="text-center mb-4">
          {hint && <p className="text-xs text-[#77786f]">{t('Gợi ý', 'Hint')}: {hint}</p>}
          {meaning && <p className="text-xs font-semibold text-[#69734f] mt-1">{t('Nghĩa', 'Meaning')}: {meaning}</p>}
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
  const { translate: t, language } = useLanguage();
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
        <p className="text-xs font-semibold text-[#55574f]">
          {t('Đang tải bảng xếp hạng...', 'Loading leaderboard...')}
        </p>
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <div className="leaderboard-panel text-center py-12">
        <Trophy size={42} className="text-[#aeb89a] mx-auto mb-3" />
        <h3 className="text-base font-bold text-[#252820] mb-1">
          {t('Chưa có người chơi', 'No players yet')}
        </h3>
        <p className="text-xs text-[#77786f]">
          {t(
            'Chưa có dữ liệu bảng xếp hạng. Hãy là người đầu tiên tham gia mini-game để ghi danh!',
            'No leaderboard entries yet. Be the first to complete a mini-game and claim #1!'
          )}
        </p>
      </div>
    );
  }

  const locale = language === 'vi' ? 'vi-VN' : 'en-US';

  return (
    <section>
      {/* Podium Top 3 */}
      <div className="leaderboard-podium">
        {topThree[1] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#2</span>
            <strong>{topThree[1].displayName}</strong>
            <span>{topThree[1].highScore.toLocaleString(locale)} pts</span>
          </div>
        )}

        {topThree[0] && (
          <div className="leaderboard-podium-card rank-1">
            <span className="leaderboard-medal">🏆</span>
            <strong>{topThree[0].displayName}</strong>
            <span>{topThree[0].highScore.toLocaleString(locale)} pts</span>
          </div>
        )}

        {topThree[2] && (
          <div className="leaderboard-podium-card">
            <span className="leaderboard-medal">#3</span>
            <strong>{topThree[2].displayName}</strong>
            <span>{topThree[2].highScore.toLocaleString(locale)} pts</span>
          </div>
        )}
      </div>

      {/* Leaderboard Rows */}
      <div className="leaderboard-panel mt-6">
        <div className="leaderboard-panel-heading mb-4">
          <h2>{t('Bảng vàng danh dự', 'Hall of Fame')}</h2>
          <span className="text-xs text-[#77786f]">
            {t('Đồng bộ từ Database xếp hạng', 'Synced live from Database')}
          </span>
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
                <small>{t(`Thắng ${entry.totalWins} trận`, `${entry.totalWins} wins`)}</small>
              </div>
              <span className="leaderboard-xp font-semibold">
                {entry.highScore.toLocaleString(locale)} pts
              </span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
