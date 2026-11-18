"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Bot,
  Check,
  Flame,
  Gauge,
  LoaderCircle,
  MessageCircle,
  Radio,
  Shield,
  Sparkles,
  Swords,
  Timer,
  Trophy,
  X,
  Zap,
} from "lucide-react";
import {
  ARENA_MOCK_OPPONENT,
  ARENA_MOCK_QUESTIONS,
  type ArenaQuestion,
} from "@/data/arena-mock-data";

type ArenaPhase = "lobby" | "countdown" | "playing" | "finished";

interface AnswerReview {
  questionId: string;
  selected: string;
  correct: boolean;
}

const QUESTIONS = ARENA_MOCK_QUESTIONS;
const OPPONENT = ARENA_MOCK_OPPONENT;

function formatTime(seconds: number) {
  return `0:${String(Math.max(0, seconds)).padStart(2, "0")}`;
}

export default function ArenaPage() {
  const [phase, setPhase] = useState<ArenaPhase>("lobby");
  const [countdown, setCountdown] = useState(3);
  const [questionIndex, setQuestionIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(18);
  const [score, setScore] = useState(0);
  const [opponentScore, setOpponentScore] = useState(0);
  const [opponentProgress, setOpponentProgress] = useState(0);
  const [selectedAnswer, setSelectedAnswer] = useState<string | null>(null);
  const [reviews, setReviews] = useState<AnswerReview[]>([]);
  const [lingLingNote, setLingLingNote] = useState("Mình sẽ theo dõi lỗi sai và tốc độ phản xạ của bạn.");
  const [isSearching, setIsSearching] = useState(false);
  const [chatOpen, setChatOpen] = useState(false);
  const [chatInput, setChatInput] = useState("");
  const [chatMessages, setChatMessages] = useState([
    { sender: "bot", text: "Mình là LingLing. Cần mẹo nhớ từ hoặc giải thích câu nào thì hỏi mình nhé!" },
  ]);
  const searchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const question = QUESTIONS[questionIndex];
  const answeredCount = reviews.length;
  const accuracy = answeredCount === 0 ? 0 : Math.round((score / answeredCount) * 100);

  const finishMatch = useCallback(() => {
    setPhase("finished");
    setSelectedAnswer(null);
    setLingLingNote(
      score >= opponentScore
        ? "Bạn thắng nhờ giữ nhịp ổn định. Hãy xem lại các từ chưa chắc để tăng độ bền ghi nhớ."
        : "Đối thủ nhanh hơn một nhịp. Mình đã đánh dấu các từ cần ôn lại ngay sau trận.",
    );
  }, [opponentScore, score]);

  useEffect(() => {
    if (phase !== "countdown") return;
    const timer = window.setInterval(() => {
      setCountdown((value) => {
        if (value <= 1) {
          window.clearInterval(timer);
          setPhase("playing");
          return 3;
        }
        return value - 1;
      });
    }, 850);
    return () => window.clearInterval(timer);
  }, [phase]);

  useEffect(() => {
    if (phase !== "playing") return;
    const timer = window.setInterval(() => {
      setTimeLeft((value) => {
        if (value <= 1) {
          if (questionIndex >= QUESTIONS.length - 1) finishMatch();
          else {
            setQuestionIndex((index) => index + 1);
            setSelectedAnswer(null);
          }
          return 18;
        }
        return value - 1;
      });
    }, 1000);
    return () => window.clearInterval(timer);
  }, [finishMatch, phase, questionIndex]);

  useEffect(() => {
    if (phase !== "playing") return;
    const timer = window.setInterval(() => {
      setOpponentProgress((current) => {
        const next = Math.min(QUESTIONS.length, current + (Math.random() > 0.62 ? 1 : 0));
        if (next > current && Math.random() > 0.28) setOpponentScore((value) => value + 1);
        if (next >= QUESTIONS.length && questionIndex >= QUESTIONS.length - 1) finishMatch();
        return next;
      });
    }, 2400);
    return () => window.clearInterval(timer);
  }, [finishMatch, phase, questionIndex]);

  useEffect(() => () => {
    if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
  }, []);

  const startMatch = () => {
    setIsSearching(true);
    searchTimerRef.current = setTimeout(() => {
      setIsSearching(false);
      setPhase("countdown");
      setQuestionIndex(0);
      setTimeLeft(18);
      setScore(0);
      setOpponentScore(0);
      setOpponentProgress(0);
      setReviews([]);
      setSelectedAnswer(null);
      setLingLingNote("Trận đấu bắt đầu! Đọc kỹ nghĩa, nhưng đừng để mất nhịp.");
    }, 900);
  };

  const chooseAnswer = (answer: string) => {
    if (phase !== "playing" || selectedAnswer) return;
    const correct = answer === question.answer;
    setSelectedAnswer(answer);
    setScore((value) => value + (correct ? 1 : 0));
    setReviews((items) => [...items, { questionId: question.id, selected: answer, correct }]);
    setLingLingNote(
      correct
        ? "Chính xác! Bạn đang kết nối từ với ngữ cảnh khá tốt."
        : `Mình ghi nhận lỗi "${question.prompt}". Đáp án đúng là "${question.answer}".`,
    );
    window.setTimeout(() => {
      if (questionIndex >= QUESTIONS.length - 1) finishMatch();
      else {
        setQuestionIndex((index) => index + 1);
        setSelectedAnswer(null);
        setTimeLeft(18);
      }
    }, 700);
  };

  const sendChat = () => {
    const text = chatInput.trim();
    if (!text) return;
    setChatMessages((items) => [
      ...items,
      { sender: "user", text },
      {
        sender: "bot",
        text: text.toLowerCase().includes("curious")
          ? '“Curious” = “tò mò”. Hãy nhớ cụm “curious about” = tò mò về điều gì.'
          : "Mình sẽ phân tích sau lượt này. Mẹo nhanh: hãy đọc cả từ khóa và ngữ cảnh trước khi chọn đáp án.",
      },
    ]);
    setChatInput("");
  };

  const weakWords = useMemo(
    () => reviews.filter((review) => !review.correct).map((review) => QUESTIONS.find((item) => item.id === review.questionId)?.prompt).filter(Boolean),
    [reviews],
  );

  return (
    <main className="arena-page">
      <header className="arena-heading">
        <div>
          <p className="arena-eyebrow"><Swords size={14} /> REAL-TIME VOCABULARY DUEL</p>
          <h1>Đấu trường Arena</h1>
          <p>Đấu 1vs1 từ vựng thời gian thực, phản xạ nhanh hơn và học sâu hơn sau mỗi lượt.</p>
        </div>
        <div className="arena-live-badge"><Radio size={14} /> Máy chủ ổn định <strong>24ms</strong></div>
      </header>

      {phase === "lobby" && (
        <section className="arena-lobby">
          <div className="arena-lobby-main">
            <div className="arena-hero-icon"><Swords size={32} /></div>
            <p className="arena-eyebrow">MỞ PHÒNG ĐẤU</p>
            <h2>Sẵn sàng so tài?</h2>
            <p>5 câu hỏi · 18 giây mỗi câu · Ghép đối thủ cùng trình độ</p>
            <button className="arena-primary-button" onClick={startMatch} disabled={isSearching}>
              {isSearching ? <><LoaderCircle className="spin" size={17} /> Đang tìm đối thủ…</> : <><Zap size={17} /> Tìm đối thủ</>}
            </button>
            <div style={{ marginTop: "10px" }}>
              <Link href="/duel" className="arena-primary-button" style={{ textDecoration: "none", display: "inline-flex", alignItems: "center", gap: "6px", background: "linear-gradient(135deg, #10B981, #059669)", width: "100%", justifyContent: "center" }}>
                <Swords size={17} /> Đấu trường Realtime SignalR
              </Link>
            </div>
            <div className="arena-lobby-stats"><span><UsersIcon /> 128 người đang online</span><span><Gauge size={14} /> Ping trung bình 32ms</span></div>
          </div>
          <aside className="arena-rules">
            <p className="arena-eyebrow">LUẬT ARENA</p>
            <div><Timer size={17} /><span><strong>18 giây</strong><small>cho mỗi câu hỏi</small></span></div>
            <div><Trophy size={17} /><span><strong>+25 XP</strong><small>khi thắng trận</small></span></div>
            <div><Sparkles size={17} /><span><strong>LingLing AI</strong><small>phân tích lỗi sau trận</small></span></div>
          </aside>
        </section>
      )}

      {phase === "countdown" && (
        <section className="arena-countdown">
          <p>Đối thủ đã sẵn sàng</p>
          <strong>{countdown}</strong>
          <span>Chuẩn bị chiến đấu!</span>
        </section>
      )}

      {(phase === "playing" || phase === "finished") && (
        <section className="arena-match">
          <div className="arena-match-topbar">
            <span className="arena-round"><Flame size={15} /> Vòng {Math.min(questionIndex + 1, QUESTIONS.length)} / {QUESTIONS.length}</span>
            <span className="arena-timer"><Timer size={15} /> {phase === "finished" ? "KẾT THÚC" : formatTime(timeLeft)}</span>
            <span className="arena-streak"><Zap size={14} /> Chuỗi {score}</span>
          </div>
          <div className="arena-scoreboard">
            <PlayerCard name="Bạn" initials="PT" score={score} progress={answeredCount} isCurrent />
            <div className="arena-vs">VS</div>
            <PlayerCard name={OPPONENT.name} initials={OPPONENT.initials} score={opponentScore} progress={opponentProgress} />
          </div>

          {phase === "playing" && (
            <div className="arena-question-card">
              <span className="arena-question-category">{question.category}</span>
              <h2>{question.prompt}</h2>
              <p>{question.definition}</p>
              <div className="arena-options">
                {question.options.map((option) => {
                  const isSelected = selectedAnswer === option;
                  const isCorrect = option === question.answer;
                  const state = selectedAnswer && (isSelected ? (isCorrect ? "correct" : "wrong") : isCorrect ? "reveal" : "") || "";
                  return <button key={option} className={`arena-option ${state}`} onClick={() => chooseAnswer(option)} disabled={Boolean(selectedAnswer)}>{state === "correct" && <Check size={16} />}{state === "wrong" && <X size={16} />}{!state && <span className="arena-option-key">{String.fromCharCode(65 + question.options.indexOf(option))}</span>}{option}</button>;
                })}
              </div>
            </div>
          )}

          {phase === "finished" && (
            <div className="arena-result-card">
              <div className={`arena-result-icon ${score >= opponentScore ? "win" : "loss"}`}>{score >= opponentScore ? <Trophy size={28} /> : <Shield size={28} />}</div>
              <p className="arena-eyebrow">KẾT QUẢ TRẬN ĐẤU</p>
              <h2>{score >= opponentScore ? "Bạn đã chiến thắng!" : "Một trận đấu đáng nhớ!"}</h2>
              <div className="arena-final-score"><strong>{score}</strong><span>—</span><strong>{opponentScore}</strong></div>
              <p>{score >= opponentScore ? "+25 XP · +1 chiến thắng" : "+10 XP · Kinh nghiệm là sức mạnh"} · Độ chính xác {accuracy}%</p>
              {weakWords.length > 0 && <div className="arena-mistake-summary"><strong>LingLing đánh dấu cần ôn:</strong> {weakWords.join(", ")}</div>}
              <button className="arena-primary-button" onClick={() => setPhase("lobby")}><Swords size={17} /> Đấu trận mới</button>
            </div>
          )}

          <div className="arena-insight-bar"><Bot size={17} /><span><strong>LingLing:</strong> {lingLingNote}</span><button onClick={() => setChatOpen(true)}><MessageCircle size={15} /> Hỏi LingLing</button></div>
        </section>
      )}

      {chatOpen && <aside className="arena-chat-panel" aria-label="Chatbot LingLing">
        <div className="arena-chat-header"><div><Bot size={18} /><strong>LingLing AI</strong><small>Đang phân tích trận đấu</small></div><button onClick={() => setChatOpen(false)} aria-label="Đóng chat"><X size={17} /></button></div>
        <div className="arena-chat-messages">{chatMessages.map((message, index) => <div key={`${message.sender}-${index}`} className={`arena-chat-message ${message.sender}`}>{message.text}</div>)}</div>
        <div className="arena-chat-input"><input value={chatInput} onChange={(event) => setChatInput(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") sendChat(); }} placeholder="Hỏi về lỗi sai hoặc từ vựng…" /><button onClick={sendChat} aria-label="Gửi tin nhắn"><MessageCircle size={16} /></button></div>
      </aside>}
    </main>
  );
}

function PlayerCard({ name, initials, score, progress, isCurrent = false }: { name: string; initials: string; score: number; progress: number; isCurrent?: boolean }) {
  return <div className={`arena-player ${isCurrent ? "is-current" : ""}`}><div className="arena-player-avatar">{initials}</div><div className="arena-player-info"><strong>{name}</strong><small>{isCurrent ? "Bạn" : `${OPPONENT.level} · 🔥 ${OPPONENT.streak}`}</small></div><strong className="arena-player-score">{score}</strong><div className="arena-player-progress"><span style={{ width: `${Math.min(100, (progress / QUESTIONS.length) * 100)}%` }} /></div></div>;
}

function UsersIcon() {
  return <span className="arena-users-icon" aria-hidden="true">●●●</span>;
}
