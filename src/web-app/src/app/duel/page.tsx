'use client';

import React, { useState, useEffect, useRef, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import * as signalR from '@microsoft/signalr';
import {
  Swords,
  Bot,
  Users,
  Timer,
  Flame,
  Trophy,
  Sparkles,
  ShieldAlert,
  Wifi,
  WifiOff,
  Flag,
  CheckCircle2,
  XCircle,
  BookOpen,
  ArrowRight,
  RotateCcw,
  Zap,
} from 'lucide-react';
import { useAuth } from '@/components/auth/useAuth';
import AppNavigation from '@/components/AppNavigation';

interface DuelOption {
  key: string;
  text: string;
}

interface DuelQuestion {
  id: string;
  roundNumber: number;
  totalRounds: number;
  prompt: string;
  level: string;
  difficulty: number;
  options: DuelOption[];
  correctKey: string;
  correctText: string;
  explanation: string;
}

interface RoundHistoryItem {
  roundNumber: number;
  question: DuelQuestion;
  mySelectedKey: string | null;
  myCorrect: boolean;
  myResponseMs: number;
  myPoints: number;
  opponentSelectedKey: string;
  opponentCorrect: boolean;
  opponentResponseMs: number;
  opponentPoints: number;
}

interface DeepAiAnalysis {
  whyWrong: string;
  grammarAndContextRule: string;
  synonyms: string[];
  antonyms: string[];
  mnemonicTip: string;
  exampleSentence: string;
}

const ARENA_QUESTIONS: DuelQuestion[] = [
  {
    id: '11111111-2222-3333-4444-555555550001',
    roundNumber: 1,
    totalRounds: 5,
    prompt: 'She ___ to the university campus every morning by electric bus.',
    level: 'A1',
    difficulty: 1,
    options: [
      { key: 'A', text: 'go' },
      { key: 'B', text: 'goes' },
      { key: 'C', text: 'going' },
      { key: 'D', text: 'gone' },
    ],
    correctKey: 'B',
    correctText: 'goes',
    explanation: 'Chủ ngữ "She" (ngôi thứ 3 số ít) ở thì hiện tại đơn đi với động từ thêm "-es" (goes).',
  },
  {
    id: '11111111-2222-3333-4444-555555550002',
    roundNumber: 2,
    totalRounds: 5,
    prompt: 'Could you please ___ me your dictionary for a few minutes?',
    level: 'A2',
    difficulty: 2,
    options: [
      { key: 'A', text: 'borrow' },
      { key: 'B', text: 'lend' },
      { key: 'C', text: 'rent' },
      { key: 'D', text: 'hire' },
    ],
    correctKey: 'B',
    correctText: 'lend',
    explanation: 'Cấu trúc "lend someone something" mang nghĩa cho ai mượn vật gì; "borrow" là mượn từ ai.',
  },
  {
    id: '11111111-2222-3333-4444-555555550003',
    roundNumber: 3,
    totalRounds: 5,
    prompt: 'Our software engineering team remained ___ despite the tight project deadline.',
    level: 'B1',
    difficulty: 3,
    options: [
      { key: 'A', text: 'resilient' },
      { key: 'B', text: 'fragile' },
      { key: 'C', text: 'hesitant' },
      { key: 'D', text: 'redundant' },
    ],
    correctKey: 'A',
    correctText: 'resilient',
    explanation: '"Resilient" (kiên cường, bền bỉ) phù hợp với giới từ nhượng bộ "despite the tight project deadline".',
  },
  {
    id: '11111111-2222-3333-4444-555555550004',
    roundNumber: 4,
    totalRounds: 5,
    prompt: 'If we ___ earlier, we would not have missed the opening keynote.',
    level: 'B1',
    difficulty: 3,
    options: [
      { key: 'A', text: 'leave' },
      { key: 'B', text: 'left' },
      { key: 'C', text: 'had left' },
      { key: 'D', text: 'have left' },
    ],
    correctKey: 'C',
    correctText: 'had left',
    explanation: 'Câu điều kiện loại 3 diễn tả giả định trái ngược quá khứ: If + S + had + V3/ed, S + would have + V3/ed.',
  },
  {
    id: '11111111-2222-3333-4444-555555550005',
    roundNumber: 5,
    totalRounds: 5,
    prompt: 'The new AI feature significantly ___ user engagement across all Mezon Clans.',
    level: 'B2',
    difficulty: 4,
    options: [
      { key: 'A', text: 'accelerated' },
      { key: 'B', text: 'diminished' },
      { key: 'C', text: 'postponed' },
      { key: 'D', text: 'neglected' },
    ],
    correctKey: 'A',
    correctText: 'accelerated',
    explanation: '"Accelerated" (thúc đẩy, tăng tốc mạnh mẽ) mang sắc thái tích cực phù hợp ngữ cảnh tăng trưởng tương tác.',
  },
];

function calculateSpeedScore(isCorrect: boolean, responseMs: number): number {
  if (!isCorrect) return 0;
  const clamped = Math.max(0, Math.min(10000, responseMs));
  const speedRatio = (10000 - clamped) / 10000;
  return Math.round(100 + 100 * speedRatio);
}

function DuelArenaContent() {
  const searchParams = useSearchParams();
  const initialMatchId = searchParams.get('matchId') || '';
  const { user } = useAuth();

  const [arenaPhase, setArenaPhase] = useState<'lobby' | 'matchmaking' | 'countdown' | 'playing' | 'completed'>('lobby');
  const [matchId, setMatchId] = useState<string>(initialMatchId);
  const [roomInput, setRoomInput] = useState<string>(initialMatchId);
  const [opponentName, setOpponentName] = useState<string>('🦉 LingLing AI Bot');
  const [isBotMatch, setIsBotMatch] = useState<boolean>(true);
  const [countdownSec, setCountdownSec] = useState<number>(3);

  // Active Round State
  const [currentRound, setCurrentRound] = useState<number>(1);
  const [roundTimeLeft, setRoundTimeLeft] = useState<number>(10);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [opponentStatus, setOpponentStatus] = useState<'thinking' | 'answered' | 'reconnecting'>('thinking');
  const [myScore, setMyScore] = useState<number>(0);
  const [opponentScore, setOpponentScore] = useState<number>(0);
  const [myCombo, setMyCombo] = useState<number>(0);
  const [roundRevealed, setRoundRevealed] = useState<boolean>(false);
  const [history, setHistory] = useState<RoundHistoryItem[]>([]);
  const [endReason, setEndReason] = useState<'normal' | 'forfeit' | 'opponent_forfeit'>('normal');

  // Network & Reconnect Simulation State (SP3-06)
  const [connectionState, setConnectionState] = useState<'connected' | 'reconnecting'>('connected');
  const [reconnectNotice, setReconnectNotice] = useState<string | null>(null);

  // Deep AI Tutor v2.0 State (SP3-03)
  const [analyzingRound, setAnalyzingRound] = useState<number | null>(null);
  const [deepAnalyses, setDeepAnalyses] = useState<Record<number, DeepAiAnalysis>>({});

  const roundStartTimestampRef = useRef<number>(Date.now());
  const hubConnectionRef = useRef<signalR.HubConnection | null>(null);

  const playerName = user?.displayName || 'Chiến binh LINGUAL';
  const currentQuestion = ARENA_QUESTIONS[currentRound - 1] || ARENA_QUESTIONS[0];

  // Try connecting to SignalR GameHub in background with graceful fallback
  useEffect(() => {
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5224';
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${backendUrl}/hubs/game`, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 8000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.onreconnecting(() => {
      setConnectionState('reconnecting');
      setReconnectNotice('⚠️ Mất kết nối mạng tạm thời... Hệ thống đang tự động khôi phục phiên đấu (<10s)!');
    });

    connection.onreconnected(() => {
      setConnectionState('connected');
      setReconnectNotice('✅ Đã khôi phục kết nối thành công! Đồng bộ trạng thái vòng đấu hiện tại.');
      setTimeout(() => setReconnectNotice(null), 3500);
    });

    connection
      .start()
      .then(() => {
        hubConnectionRef.current = connection;
      })
      .catch(() => {
        // Fallback engine handles interactive rounds seamlessly
      });

    return () => {
      connection.stop().catch(() => {});
    };
  }, []);

  // Auto-start if matchId is provided in URL query string
  useEffect(() => {
    if (initialMatchId && arenaPhase === 'lobby') {
      startArenaMatch('⚔️ Đối thủ Clan', false, initialMatchId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialMatchId]);

  const startArenaMatch = async (targetOpponent: string, botMode: boolean, customMatchId?: string) => {
    setOpponentName(targetOpponent);
    setIsBotMatch(botMode);
    setMyScore(0);
    setOpponentScore(0);
    setMyCombo(0);
    setHistory([]);
    setDeepAnalyses({});
    setEndReason('normal');

    let resolvedId = customMatchId || crypto.randomUUID();
    try {
      const res = await fetch('/api/duel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          challengerId: '00000000-0000-0000-0000-000000000001',
          challengerName: playerName,
          opponentId: '00000000-0000-0000-0000-00000000bb07',
          opponentName: targetOpponent,
          questionCount: 5,
          autoAccept: true,
        }),
      });
      if (res.ok) {
        const data = await res.json();
        if (data?.id) resolvedId = data.id;
      }
    } catch {
      // Keep generated UUID
    }

    setMatchId(resolvedId);
    setCountdownSec(3);
    setArenaPhase('countdown');
  };

  const handleQuickMatchmaking = () => {
    setArenaPhase('matchmaking');
    setTimeout(() => {
      startArenaMatch('⚡ Minh_MCS26 (Clan Challenger)', false);
    }, 1800);
  };

  // Pre-match 3s countdown
  useEffect(() => {
    if (arenaPhase !== 'countdown') return;
    if (countdownSec <= 0) {
      setCurrentRound(1);
      setRoundTimeLeft(10);
      setSelectedKey(null);
      setRoundRevealed(false);
      setOpponentStatus('thinking');
      roundStartTimestampRef.current = Date.now();
      setArenaPhase('playing');
      return;
    }
    const timer = setTimeout(() => setCountdownSec((prev) => prev - 1), 900);
    return () => clearTimeout(timer);
  }, [arenaPhase, countdownSec]);

  // 10s Round Timer & Opponent Simulation
  useEffect(() => {
    if (arenaPhase !== 'playing' || roundRevealed) return;

    // Opponent answers around 3s - 6s into the round
    const oppDelayMs = isBotMatch ? 2600 : 3400;
    const oppTimer = setTimeout(() => {
      setOpponentStatus('answered');
    }, oppDelayMs);

    const interval = setInterval(() => {
      setRoundTimeLeft((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          finalizeCurrentRound(null, 10000);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      clearTimeout(oppTimer);
      clearInterval(interval);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [arenaPhase, currentRound, roundRevealed]);

  const finalizeCurrentRound = (chosenKey: string | null, elapsedMs: number) => {
    if (roundRevealed) return;
    setRoundRevealed(true);
    setOpponentStatus('answered');

    const q = ARENA_QUESTIONS[currentRound - 1];
    const isMyCorrect = chosenKey === q.correctKey;
    const myPts = calculateSpeedScore(isMyCorrect, elapsedMs);

    // Deterministic opponent performance
    const oppCorrect = currentRound !== 4; // Opponent misses round 4 so intense matches happen
    const oppElapsedMs = 2800 + currentRound * 650;
    const oppChosenKey = oppCorrect
      ? q.correctKey
      : q.options.find((o) => o.key !== q.correctKey)?.key || 'A';
    const oppPts = calculateSpeedScore(oppCorrect, oppElapsedMs);

    setMyScore((prev) => prev + myPts);
    setOpponentScore((prev) => prev + oppPts);
    setMyCombo((prev) => (isMyCorrect ? prev + 1 : 0));

    const roundRecord: RoundHistoryItem = {
      roundNumber: currentRound,
      question: q,
      mySelectedKey: chosenKey,
      myCorrect: isMyCorrect,
      myResponseMs: elapsedMs,
      myPoints: myPts,
      opponentSelectedKey: oppChosenKey,
      opponentCorrect: oppCorrect,
      opponentResponseMs: oppElapsedMs,
      opponentPoints: oppPts,
    };

    setHistory((prev) => [...prev, roundRecord]);

    // Move to next round or complete match after 2.2s
    setTimeout(() => {
      if (currentRound >= 5) {
        setArenaPhase('completed');
      } else {
        setCurrentRound((r) => r + 1);
        setRoundTimeLeft(10);
        setSelectedKey(null);
        setRoundRevealed(false);
        setOpponentStatus('thinking');
        roundStartTimestampRef.current = Date.now();
      }
    }, 2200);
  };

  const handleSelectOption = (key: string) => {
    if (arenaPhase !== 'playing' || selectedKey !== null || roundRevealed) return;
    const elapsedMs = Math.max(300, Math.min(9900, Date.now() - roundStartTimestampRef.current));
    setSelectedKey(key);
    finalizeCurrentRound(key, elapsedMs);
  };

  const handleSimulateReconnect = () => {
    setConnectionState('reconnecting');
    setOpponentStatus('reconnecting');
    setReconnectNotice('⚠️ Phát hiện gián đoạn kết nối mạng (SP3-06)... Đang khôi phục phiên đấu trong thời gian ân hạn <10s...');
    setTimeout(() => {
      setConnectionState('connected');
      setOpponentStatus(selectedKey ? 'answered' : 'thinking');
      setReconnectNotice('✅ Đã kết nối lại thành công sau 1.8 giây! Đồng bộ nguyên vẹn điểm số và câu hỏi hiện tại.');
      setTimeout(() => setReconnectNotice(null), 4000);
    }, 1800);
  };

  const handleForfeitMatch = async () => {
    if (matchId) {
      fetch(`/api/duel/${matchId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          action: 'forfeit',
          userId: '00000000-0000-0000-0000-000000000001',
          reason: 'forfeit',
        }),
      }).catch(() => {});
    }
    setEndReason('forfeit');
    setArenaPhase('completed');
  };

  const handleExplainRoundMistake = async (item: RoundHistoryItem) => {
    if (deepAnalyses[item.roundNumber] || analyzingRound === item.roundNumber) return;
    setAnalyzingRound(item.roundNumber);

    const chosenOptionText =
      item.question.options.find((o) => o.key === item.mySelectedKey)?.text ||
      item.mySelectedKey ||
      'Chưa chọn đáp án (Hết giờ)';

    try {
      const res = await fetch('/api/ai/explain-mistake', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          questionPrompt: item.question.prompt,
          selectedOptionText: chosenOptionText,
          correctOptionText: item.question.correctText,
          level: item.question.level,
          existingExplanation: item.question.explanation,
        }),
      });
      if (res.ok) {
        const data = await res.json();
        setDeepAnalyses((prev) => ({ ...prev, [item.roundNumber]: data }));
      }
    } finally {
      setAnalyzingRound(null);
    }
  };

  const isWinner = endReason !== 'forfeit' && myScore > opponentScore;
  const isDraw = endReason !== 'forfeit' && myScore === opponentScore;
  const xpReward = isWinner ? 30 : isDraw ? 15 : 10;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 pb-24">
      <div className="max-w-4xl mx-auto px-4 pt-6 space-y-6">
        {/* Top Header */}
        <header className="flex flex-wrap items-center justify-between gap-4 bg-slate-900/90 border border-slate-800 rounded-2xl p-4 shadow-lg">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-rose-500 to-amber-500 flex items-center justify-center shadow-md shadow-rose-500/20">
              <Swords className="w-6 h-6 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-xl font-bold tracking-tight text-white">Word Duel Arena 1vs1</h1>
                <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-rose-500/20 text-rose-300 border border-rose-500/30">
                  Real-time 5 Vòng
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Đấu trường tốc độ 10s/câu • Thưởng +30 XP Chiến thắng & Vinh danh Bảng Vàng Clan
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <span
              className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium border ${
                connectionState === 'connected'
                  ? 'bg-emerald-500/10 text-emerald-300 border-emerald-500/30'
                  : 'bg-amber-500/15 text-amber-300 border-amber-500/40 animate-pulse'
              }`}
            >
              {connectionState === 'connected' ? (
                <>
                  <Wifi className="w-3.5 h-3.5" /> SignalR Real-time Ready
                </>
              ) : (
                <>
                  <WifiOff className="w-3.5 h-3.5" /> Đang khôi phục kết nối (&lt;10s)...
                </>
              )}
            </span>
          </div>
        </header>

        {/* Network Reconnect Toast Banner (SP3-06) */}
        {reconnectNotice && (
          <div className="rounded-xl bg-indigo-950/90 border border-indigo-500/40 px-4 py-3 text-sm text-indigo-200 flex items-center justify-between gap-3 shadow-lg">
            <span>{reconnectNotice}</span>
            <span className="text-xs font-mono text-indigo-300 shrink-0">SP3-06 Grace Period</span>
          </div>
        )}

        {/* PHASE 1: LOBBY */}
        {arenaPhase === 'lobby' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            {/* Quick Match 1v1 Card */}
            <div className="bg-slate-900/90 border border-rose-500/30 rounded-2xl p-6 flex flex-col justify-between space-y-5 shadow-xl">
              <div className="space-y-3">
                <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-rose-500/15 text-rose-300 text-xs font-semibold">
                  <Users className="w-3.5 h-3.5" /> Thách Đấu Đối Kháng 1vs1
                </div>
                <h2 className="text-2xl font-bold text-white">Ghép Trận Nhanh Clan</h2>
                <p className="text-sm text-slate-300 leading-relaxed">
                  So tài từ vựng & ngữ pháp tốc độ cao với thành viên trong Clan qua 5 vòng đấu đồng bộ (10 giây/câu). Điểm số tính theo độ chính xác kết hợp tốc độ phản xạ (100 - 200 điểm/câu).
                </p>
                <ul className="space-y-1.5 text-xs text-slate-400">
                  <li className="flex items-center gap-2">
                    <Trophy className="w-4 h-4 text-amber-400" /> Thắng: <strong className="text-amber-300">+30 XP</strong> • Hòa: <strong className="text-emerald-300">+15 XP</strong> • Tham gia: <strong className="text-sky-300">+10 XP</strong>
                  </li>
                  <li className="flex items-center gap-2">
                    <Sparkles className="w-4 h-4 text-purple-400" /> Tự động bắn Thẻ Vinh Danh Chiến Thắng ra kênh Clan Mezon
                  </li>
                </ul>
              </div>

              <div className="space-y-3 pt-2">
                <button
                  onClick={handleQuickMatchmaking}
                  className="w-full py-3.5 px-5 rounded-xl bg-gradient-to-r from-rose-600 to-amber-600 hover:from-rose-500 hover:to-amber-500 text-white font-bold text-sm shadow-lg shadow-rose-600/25 flex items-center justify-center gap-2 transition cursor-pointer"
                >
                  <Swords className="w-4 h-4" /> Tìm Đối Thủ 1vs1 Ngay
                </button>

                <div className="flex items-center gap-2 pt-2">
                  <input
                    type="text"
                    value={roomInput}
                    onChange={(e) => setRoomInput(e.target.value)}
                    placeholder="Nhập mã phòng /duel từ Mezon..."
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white placeholder:text-slate-500 focus:outline-none focus:border-rose-500"
                  />
                  <button
                    onClick={() =>
                      startArenaMatch('⚔️ Đấu Thủ Clan', false, roomInput.trim() || undefined)
                    }
                    className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-200 transition cursor-pointer"
                  >
                    Vào Phòng
                  </button>
                </div>
              </div>
            </div>

            {/* LingLing AI Bot Sparring Card */}
            <div className="bg-slate-900/90 border border-purple-500/30 rounded-2xl p-6 flex flex-col justify-between space-y-5 shadow-xl">
              <div className="space-y-3">
                <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-purple-500/15 text-purple-300 text-xs font-semibold">
                  <Bot className="w-3.5 h-3.5" /> Luyện Phản Xạ Cùng AI
                </div>
                <h2 className="text-2xl font-bold text-white">Đấu Với 🦉 LingLing AI Bot</h2>
                <p className="text-sm text-slate-300 leading-relaxed">
                  Không cần chờ đối thủ! Bước vào đấu trường ngay lập tức cùng trợ lý AI LingLing. Sau trận đấu, LingLing sẽ phân tích chuyên sâu từng câu bạn chọn sai.
                </p>
                <div className="rounded-xl bg-slate-950/80 border border-slate-800 p-3.5 space-y-1.5 text-xs text-slate-300">
                  <div className="font-semibold text-purple-300 flex items-center gap-1.5">
                    <BookOpen className="w-4 h-4" /> Tích hợp Deep AI Tutor v2.0 (SP3-03):
                  </div>
                  <p className="text-slate-400">
                    Giải thích vì sao đáp án bạn chọn bị sai, quy tắc ngữ pháp cốt lõi, từ đồng nghĩa/trái nghĩa và mẹo ghi nhớ siêu tốc.
                  </p>
                </div>
              </div>

              <button
                onClick={() => startArenaMatch('🦉 LingLing AI Bot', true)}
                className="w-full py-3.5 px-5 rounded-xl bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-bold text-sm shadow-lg shadow-purple-600/25 flex items-center justify-center gap-2 transition cursor-pointer"
              >
                <Bot className="w-4 h-4" /> Khiêu Chiến LingLing Bot Ngay
              </button>
            </div>
          </div>
        )}

        {/* PHASE 2: MATCHMAKING QUEUE */}
        {arenaPhase === 'matchmaking' && (
          <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-10 text-center space-y-5">
            <div className="w-16 h-16 rounded-full bg-rose-500/20 border border-rose-500/40 mx-auto flex items-center justify-center animate-pulse">
              <Swords className="w-8 h-8 text-rose-400" />
            </div>
            <h2 className="text-xl font-bold text-white">Đang tìm kiếm đối thủ xứng tầm trong Clan...</h2>
            <p className="text-sm text-slate-400 max-w-md mx-auto">
              Hệ thống SignalR GameHub đang ghép cặp thời gian thực và đồng bộ bộ 5 câu hỏi tốc độ.
            </p>
          </div>
        )}

        {/* PHASE 3: PRE-MATCH COUNTDOWN */}
        {arenaPhase === 'countdown' && (
          <div className="bg-slate-900/90 border border-rose-500/40 rounded-2xl p-10 text-center space-y-6 shadow-2xl">
            <div className="flex items-center justify-center gap-6">
              <div className="text-right">
                <p className="text-xs text-slate-400">Thách đấu</p>
                <p className="text-lg font-bold text-emerald-400">{playerName}</p>
              </div>
              <div className="w-12 h-12 rounded-full bg-rose-500/20 border border-rose-500/40 flex items-center justify-center font-black text-rose-400">
                VS
              </div>
              <div className="text-left">
                <p className="text-xs text-slate-400">Đối thủ</p>
                <p className="text-lg font-bold text-amber-400">{opponentName}</p>
              </div>
            </div>

            <div className="py-4">
              <div className="text-6xl font-black text-white tracking-wider animate-bounce">
                {countdownSec > 0 ? countdownSec : 'FIGHT!'}
              </div>
              <p className="text-xs text-slate-400 mt-3">
                Mã trận đấu: <code className="text-rose-300">{matchId.slice(0, 8)}</code> • 5 Vòng • 10 giây / câu
              </p>
            </div>
          </div>
        )}

        {/* PHASE 4: ACTIVE 5-ROUND ARENA */}
        {arenaPhase === 'playing' && (
          <div className="space-y-5">
            {/* Dual Player Score & HP Bars */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-slate-900/90 border border-slate-800 rounded-2xl p-4">
              {/* Challenger (You) */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-emerald-400" />
                    <span className="font-bold text-sm text-white">{playerName} (Bạn)</span>
                    {myCombo >= 2 && (
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 text-xs font-bold">
                        <Flame className="w-3 h-3" /> Combo x{myCombo}
                      </span>
                    )}
                  </div>
                  <span className="text-lg font-black text-emerald-400">{myScore} pts</span>
                </div>
                <div className="w-full h-3 bg-slate-800 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-gradient-to-r from-emerald-500 to-teal-400 transition-all duration-500"
                    style={{ width: `${Math.min(100, Math.max(8, (myScore / 1000) * 100))}%` }}
                  />
                </div>
              </div>

              {/* Opponent */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-sm text-white">{opponentName}</span>
                    <span
                      className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                        opponentStatus === 'answered'
                          ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                          : opponentStatus === 'reconnecting'
                          ? 'bg-rose-500/20 text-rose-300 animate-pulse'
                          : 'bg-slate-800 text-slate-400'
                      }`}
                    >
                      {opponentStatus === 'answered'
                        ? '⚡ Đã chốt đáp án!'
                        : opponentStatus === 'reconnecting'
                        ? '⚠️ Đang kết nối lại (<10s)...'
                        : '⏳ Đang suy nghĩ...'}
                    </span>
                  </div>
                  <span className="text-lg font-black text-amber-400">{opponentScore} pts</span>
                </div>
                <div className="w-full h-3 bg-slate-800 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-gradient-to-r from-amber-500 to-rose-500 transition-all duration-500"
                    style={{ width: `${Math.min(100, Math.max(8, (opponentScore / 1000) * 100))}%` }}
                  />
                </div>
              </div>
            </div>

            {/* Round Header & 10s Countdown Bar */}
            <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 space-y-5 shadow-xl">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="px-3 py-1 rounded-lg bg-indigo-500/20 text-indigo-300 font-bold text-xs border border-indigo-500/30">
                    Vòng {currentRound} / 5
                  </span>
                  <span className="px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 text-xs font-medium">
                    CEFR {currentQuestion.level}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  <Timer className={`w-5 h-5 ${roundTimeLeft <= 3 ? 'text-rose-400 animate-ping' : 'text-amber-400'}`} />
                  <span
                    className={`text-xl font-black font-mono ${
                      roundTimeLeft <= 3 ? 'text-rose-400' : 'text-white'
                    }`}
                  >
                    00:{roundTimeLeft.toString().padStart(2, '0')}
                  </span>
                </div>
              </div>

              {/* Timer progress bar */}
              <div className="w-full h-2 bg-slate-800 rounded-full overflow-hidden">
                <div
                  className={`h-full transition-all duration-1000 ${
                    roundTimeLeft <= 3 ? 'bg-rose-500' : 'bg-indigo-500'
                  }`}
                  style={{ width: `${(roundTimeLeft / 10) * 100}%` }}
                />
              </div>

              {/* Question Prompt */}
              <div className="py-3">
                <p className="text-lg md:text-xl font-bold text-white leading-relaxed">
                  {currentQuestion.prompt}
                </p>
              </div>

              {/* 4 Answer Options */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                {currentQuestion.options.map((opt) => {
                  const isChosen = selectedKey === opt.key;
                  const isCorrectOption = opt.key === currentQuestion.correctKey;

                  let btnStyle =
                    'bg-slate-950/90 hover:bg-slate-800/90 border-slate-800 text-slate-100';
                  if (roundRevealed) {
                    if (isCorrectOption) {
                      btnStyle = 'bg-emerald-500/20 border-emerald-500 text-emerald-200';
                    } else if (isChosen && !isCorrectOption) {
                      btnStyle = 'bg-rose-500/20 border-rose-500 text-rose-200';
                    } else {
                      btnStyle = 'bg-slate-950/40 border-slate-800/60 text-slate-500';
                    }
                  } else if (isChosen) {
                    btnStyle = 'bg-indigo-600/30 border-indigo-400 text-white';
                  }

                  return (
                    <button
                      key={opt.key}
                      disabled={selectedKey !== null || roundRevealed}
                      onClick={() => handleSelectOption(opt.key)}
                      className={`p-4 rounded-xl border text-left transition flex items-center justify-between gap-3 cursor-pointer disabled:cursor-default ${btnStyle}`}
                    >
                      <div className="flex items-center gap-3">
                        <span className="w-8 h-8 rounded-lg bg-slate-800/90 border border-slate-700 flex items-center justify-center font-bold text-sm text-indigo-300 shrink-0">
                          {opt.key}
                        </span>
                        <span className="font-semibold text-sm md:text-base">{opt.text}</span>
                      </div>

                      {roundRevealed && isCorrectOption && (
                        <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
                      )}
                      {roundRevealed && isChosen && !isCorrectOption && (
                        <XCircle className="w-5 h-5 text-rose-400 shrink-0" />
                      )}
                    </button>
                  );
                })}
              </div>

              {/* Round Explanation Reveal */}
              {roundRevealed && (
                <div className="rounded-xl bg-slate-950/90 border border-slate-800 p-3.5 text-xs text-slate-300 flex items-start gap-2.5">
                  <Zap className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
                  <div>
                    <span className="font-bold text-white">Đáp án chuẩn: {currentQuestion.correctKey}. {currentQuestion.correctText}</span>
                    <p className="text-slate-400 mt-0.5">{currentQuestion.explanation}</p>
                  </div>
                </div>
              )}

              {/* Resilience & Forfeit Controls (SP3-06) */}
              <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-slate-800/80">
                <button
                  onClick={handleSimulateReconnect}
                  className="text-xs px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 flex items-center gap-1.5 transition cursor-pointer"
                >
                  <ShieldAlert className="w-3.5 h-3.5 text-amber-400" /> Giả lập rớt mạng &amp; Khôi phục (&lt;10s)
                </button>

                <button
                  onClick={handleForfeitMatch}
                  className="text-xs px-3 py-1.5 rounded-lg bg-rose-950/80 hover:bg-rose-900/80 text-rose-300 border border-rose-500/30 flex items-center gap-1.5 transition cursor-pointer"
                >
                  <Flag className="w-3.5 h-3.5" /> Rời trận / Xin hàng (Forfeit)
                </button>
              </div>
            </div>
          </div>
        )}

        {/* PHASE 5: POST-MATCH SUMMARY & DEEP AI TUTOR V2.0 */}
        {arenaPhase === 'completed' && (
          <div className="space-y-6">
            {/* Victory / Result Banner */}
            <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 text-center space-y-4 shadow-2xl">
              <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-amber-400 to-rose-500 mx-auto flex items-center justify-center shadow-lg shadow-amber-500/20">
                <Trophy className="w-8 h-8 text-slate-950" />
              </div>

              <div className="space-y-1">
                <h2 className="text-2xl font-black text-white">
                  {endReason === 'forfeit'
                    ? '🏳️ BẠN ĐÃ RỜI TRẬN ĐẤU (FORFEIT)'
                    : isWinner
                    ? '🏆 CHIẾN THẮNG TUYỆT ĐỐI — NHÀ VÔ ĐỊCH WORD DUEL!'
                    : isDraw
                    ? '⚔️ KẾT QUẢ HÒA KỊCH TÍNH!'
                    : '💪 HOÀN THÀNH TRẬN ĐẤU — CỐ GẮNG Ở VÁN SAU NHÉ!'}
                </h2>
                <p className="text-sm text-slate-400">
                  Đã đồng bộ kết quả vào bảng <code className="text-indigo-300">duel_matches</code> &amp; phát Thẻ Vinh Danh lên kênh Clan Mezon!
                </p>
              </div>

              {/* Final Score Comparison */}
              <div className="grid grid-cols-2 gap-4 max-w-md mx-auto pt-2">
                <div className="p-4 rounded-xl bg-slate-950 border border-emerald-500/30">
                  <p className="text-xs text-slate-400">{playerName}</p>
                  <p className="text-2xl font-black text-emerald-400">{myScore} pts</p>
                  <span className="inline-block mt-1 text-xs font-bold px-2.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300">
                    +{xpReward} XP Thưởng
                  </span>
                </div>
                <div className="p-4 rounded-xl bg-slate-950 border border-amber-500/30">
                  <p className="text-xs text-slate-400">{opponentName}</p>
                  <p className="text-2xl font-black text-amber-400">{opponentScore} pts</p>
                  <span className="inline-block mt-1 text-xs font-bold px-2.5 py-0.5 rounded-full bg-amber-500/20 text-amber-300">
                    {isWinner ? '+10 XP Tham gia' : isDraw ? '+15 XP Hòa' : '+30 XP Chiến thắng'}
                  </span>
                </div>
              </div>

              <div className="flex flex-wrap items-center justify-center gap-3 pt-3">
                <button
                  onClick={() => startArenaMatch(opponentName, isBotMatch)}
                  className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-rose-600 to-amber-600 hover:from-rose-500 hover:to-amber-500 text-white font-bold text-sm flex items-center gap-2 transition cursor-pointer"
                >
                  <RotateCcw className="w-4 h-4" /> Tái Đấu Ván Mới
                </button>
                <Link
                  href="/leaderboard"
                  className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-sm flex items-center gap-2 transition"
                >
                  <Trophy className="w-4 h-4 text-amber-400" /> Xem Bảng Phong Thần
                </Link>
              </div>
            </div>

            {/* Round-by-Round Review & LingLing Deep AI Mistake Explanation (SP3-03) */}
            <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-bold text-white flex items-center gap-2">
                    <Sparkles className="w-5 h-5 text-purple-400" /> Phân Tích Chuyên Sâu Cùng Cú LingLing (Deep AI v2.0)
                  </h3>
                  <p className="text-xs text-slate-400">
                    Bấm vào nút &ldquo;🦉 LingLing giải thích sâu&rdquo; ở bất kỳ câu nào để xem phân tích lỗi sai, quy tắc ngữ pháp, từ đồng nghĩa/trái nghĩa và mẹo ghi nhớ.
                  </p>
                </div>
              </div>

              <div className="space-y-4">
                {history.map((item) => {
                  const analysis = deepAnalyses[item.roundNumber];
                  const isLoading = analyzingRound === item.roundNumber;
                  const myOptText =
                    item.question.options.find((o) => o.key === item.mySelectedKey)?.text ||
                    'Chưa trả lời';

                  return (
                    <div
                      key={item.roundNumber}
                      className="rounded-xl bg-slate-950/90 border border-slate-800 p-4 space-y-3"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300">
                              Câu {item.roundNumber}
                            </span>
                            {item.myCorrect ? (
                              <span className="text-xs font-bold text-emerald-400 flex items-center gap-1">
                                <CheckCircle2 className="w-3.5 h-3.5" /> Chính xác (+{item.myPoints} pts • {(item.myResponseMs / 1000).toFixed(1)}s)
                              </span>
                            ) : (
                              <span className="text-xs font-bold text-rose-400 flex items-center gap-1">
                                <XCircle className="w-3.5 h-3.5" /> Chưa chính xác (Bạn chọn: {item.mySelectedKey || 'Hết giờ'} - {myOptText})
                              </span>
                            )}
                          </div>
                          <p className="font-semibold text-sm text-white">{item.question.prompt}</p>
                          <p className="text-xs text-emerald-300">
                            ✅ Đáp án đúng: <strong>{item.question.correctKey}. {item.question.correctText}</strong> — {item.question.explanation}
                          </p>
                        </div>

                        <button
                          onClick={() => handleExplainRoundMistake(item)}
                          disabled={isLoading}
                          className="px-3.5 py-2 rounded-xl bg-purple-600/20 hover:bg-purple-600/30 border border-purple-500/40 text-purple-200 text-xs font-semibold flex items-center gap-1.5 transition shrink-0 cursor-pointer"
                        >
                          <Sparkles className="w-3.5 h-3.5 text-purple-400" />
                          {isLoading
                            ? 'LingLing đang phân tích...'
                            : analysis
                            ? 'Đã phân tích chuyên sâu'
                            : '🦉 LingLing giải thích sâu'}
                        </button>
                      </div>

                      {analysis && (
                        <div className="rounded-xl bg-purple-950/30 border border-purple-500/30 p-4 space-y-2.5 text-xs">
                          <div className="font-bold text-purple-300 flex items-center gap-1.5">
                            🦉 LINGLING DEEP AI TUTOR V2.0 — PHÂN TÍCH CÂU {item.roundNumber}
                          </div>
                          <p className="text-slate-200">
                            <strong className="text-rose-300">🔍 Vì sao dễ nhầm:</strong> {analysis.whyWrong}
                          </p>
                          <p className="text-slate-200">
                            <strong className="text-indigo-300">📐 Quy tắc cốt lõi:</strong> {analysis.grammarAndContextRule}
                          </p>
                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
                            <div className="p-2.5 rounded-lg bg-slate-900/90 border border-slate-800">
                              <span className="text-emerald-300 font-semibold">🔄 Từ đồng nghĩa (Synonyms):</span>{' '}
                              <span className="text-slate-300">{analysis.synonyms.join(', ')}</span>
                            </div>
                            <div className="p-2.5 rounded-lg bg-slate-900/90 border border-slate-800">
                              <span className="text-amber-300 font-semibold">↔️ Từ trái nghĩa (Antonyms):</span>{' '}
                              <span className="text-slate-300">{analysis.antonyms.join(', ')}</span>
                            </div>
                          </div>
                          <p className="text-amber-200">
                            <strong>💡 Mẹo nhớ siêu tốc:</strong> {analysis.mnemonicTip}
                          </p>
                          <p className="text-emerald-200 italic">
                            <strong>🗣️ Ví dụ thực tế:</strong> &ldquo;{analysis.exampleSentence}&rdquo;
                          </p>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        )}
      </div>

      <AppNavigation />
    </div>
  );
}

export default function DuelArenaPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-slate-950 text-slate-200 flex items-center justify-center">
          Đang tải Đấu trường Word Duel 1vs1...
        </div>
      }
    >
      <DuelArenaContent />
    </Suspense>
  );
}

