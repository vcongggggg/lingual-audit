"use client";

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
import { useLanguage } from "@/lib/language-context";

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
  const { translate: t } = useLanguage();
  const [phase, setPhase] = useState<ArenaPhase>("lobby");
  const [countdown, setCountdown] = useState(3);
  const [questionIndex, setQuestionIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(18);
  const [score, setScore] = useState(0);
  const [opponentScore, setOpponentScore] = useState(0);
  const [opponentProgress, setOpponentProgress] = useState(0);
  const [selectedAnswer, setSelectedAnswer] = useState<string | null>(null);
  const [reviews, setReviews] = useState<AnswerReview[]>([]);
  const [lingLingNote, setLingLingNote] = useState(t("Mình sẽ theo dõi lỗi sai và tốc độ phản xạ của bạn.", "I will track your mistakes and reaction speed."));
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

  const finishMatch = useCallback((finalPlayerScore?: number) => {
    const resolvedScore = finalPlayerScore ?? score;
    setPhase("finished");
    setSelectedAnswer(null);
    setLingLingNote(
      resolvedScore >= opponentScore
        ? t("Bạn thắng nhờ giữ nhịp ổn định. Hãy xem lại các từ chưa chắc để tăng độ bền ghi nhớ.", "You won by keeping a steady rhythm. Review uncertain words to strengthen recall.")
        : t("Đối thủ nhanh hơn một nhịp. Mình đã đánh dấu các từ cần ôn lại ngay sau trận.", "Your opponent was one step faster. I marked the words to review after the match."),
    );
  }, [opponentScore, score, t]);

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
      setLingLingNote(t("Trận đấu bắt đầu! Đọc kỹ nghĩa, nhưng đừng để mất nhịp.", "The match starts now! Read the meaning carefully, but keep your rhythm."));
    }, 900);
  };

  const chooseAnswer = (answer: string) => {
    if (phase !== "playing" || selectedAnswer) return;
    const correct = answer === question.answer;
    const nextScore = score + (correct ? 1 : 0);
    setSelectedAnswer(answer);
    setScore(nextScore);
    setReviews((items) => [...items, { questionId: question.id, selected: answer, correct }]);
    setLingLingNote(
      correct
        ? t("Chính xác! Bạn đang kết nối từ với ngữ cảnh khá tốt.", "Correct! You are connecting words with context very well.")
        : t(`Mình ghi nhận lỗi "${question.prompt}". Đáp án đúng là "${question.answer}".`, `I noted a mistake with "${question.prompt}". The correct answer is "${question.answer}".`),
    );
    window.setTimeout(() => {
      if (questionIndex >= QUESTIONS.length - 1) finishMatch(nextScore);
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
          ? t('“Curious” = “tò mò”. Hãy nhớ cụm “curious about” = tò mò về điều gì.', '“Curious” means “tò mò”. Remember “curious about” for asking what someone wants to know.')
          : t("Mình sẽ phân tích sau lượt này. Mẹo nhanh: hãy đọc cả từ khóa và ngữ cảnh trước khi chọn đáp án.", "I will analyze this after the round. Quick tip: read both the keyword and context before choosing."),
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
          <p className="arena-eyebrow"><Swords size={14} /> {t("ĐẤU TỪ VỰNG THỜI GIAN THỰC", "REAL-TIME VOCABULARY DUEL")}</p>
          <h1>{t("Đấu trường Arena", "Arena duel")}</h1>
          <p>{t("Đấu 1vs1 từ vựng thời gian thực, phản xạ nhanh hơn và học sâu hơn sau mỗi lượt.", "Compete in a real-time 1v1 vocabulary duel, react faster, and learn more deeply each round.")}</p>
        </div>
        <div className="arena-live-badge"><Radio size={14} /> {t("Bản mô phỏng Bot (SignalR PvP đang phát triển)", "Bot Simulation Preview (SignalR PvP in progress)")}</div>
      </header>

      {phase === "lobby" && (
        <section className="arena-lobby">
          <div className="arena-lobby-main">
            <div className="arena-hero-icon"><Swords size={32} /></div>
            <p className="arena-eyebrow">{t("MỞ PHÒNG ĐẤU", "OPEN MATCH")}</p>
            <h2>{t("Sẵn sàng so tài?", "Ready to compete?")}</h2>
            <p>{t("5 câu hỏi · 18 giây mỗi câu · Ghép đối thủ cùng trình độ", "5 questions · 18 seconds each · Match with a similar level")}</p>
            <button className="arena-primary-button" onClick={startMatch} disabled={isSearching}>
              {isSearching ? <><LoaderCircle className="spin" size={17} /> {t("Đang tìm đối thủ…", "Finding opponent…")}</> : <><Zap size={17} /> {t("Tìm đối thủ", "Find opponent")}</>}
            </button>
            <div className="arena-lobby-stats"><span><UsersIcon /> {t("Chế độ luyện tập với Bot", "Bot practice mode")}</span><span><Gauge size={14} /> {t("Phản hồi LingLing AI tức thì", "Instant LingLing AI feedback")}</span></div>
          </div>
          <aside className="arena-rules">
            <p className="arena-eyebrow">{t("LUẬT ARENA", "ARENA RULES")}</p>
            <div><Timer size={17} /><span><strong>18 {t("giây", "seconds")}</strong><small>{t("cho mỗi câu hỏi", "per question")}</small></span></div>
            <div><Trophy size={17} /><span><strong>+25 XP</strong><small>{t("khi thắng trận", "for a win")}</small></span></div>
            <div><Sparkles size={17} /><span><strong>LingLing AI</strong><small>{t("phân tích lỗi sau trận", "post-match analysis")}</small></span></div>
          </aside>
        </section>
      )}

      {phase === "countdown" && (
        <section className="arena-countdown">
          <p>{t("Đối thủ đã sẵn sàng", "Opponent is ready")}</p>
          <strong>{countdown}</strong>
          <span>{t("Chuẩn bị chiến đấu!", "Get ready to battle!")}</span>
        </section>
      )}

      {(phase === "playing" || phase === "finished") && (
        <section className="arena-match">
          <div className="arena-match-topbar">
            <span className="arena-round"><Flame size={15} /> {t("Vòng", "Round")} {Math.min(questionIndex + 1, QUESTIONS.length)} / {QUESTIONS.length}</span>
            <span className="arena-timer"><Timer size={15} /> {phase === "finished" ? t("KẾT THÚC", "FINISHED") : formatTime(timeLeft)}</span>
            <span className="arena-streak"><Zap size={14} /> {t("Chuỗi", "Streak")} {score}</span>
          </div>
          <div className="arena-scoreboard">
            <PlayerCard name={t("Bạn", "You")} initials="PT" score={score} progress={answeredCount} isCurrent />
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
              <p className="arena-eyebrow">{t("KẾT QUẢ TRẬN ĐẤU", "MATCH RESULT")}</p>
              <h2>{score >= opponentScore ? t("Bạn đã chiến thắng!", "You won!") : t("Một trận đấu đáng nhớ!", "A memorable match!")}</h2>
              <div className="arena-final-score"><strong>{score}</strong><span>—</span><strong>{opponentScore}</strong></div>
              <p>{score >= opponentScore ? "+25 XP · " + t("+1 chiến thắng", "+1 win") : "+10 XP · " + t("Kinh nghiệm là sức mạnh", "Experience is power")} · {t("Độ chính xác", "Accuracy")} {accuracy}%</p>
              {weakWords.length > 0 && <div className="arena-mistake-summary"><strong>{t("LingLing đánh dấu cần ôn:", "LingLing marked for review:")}</strong> {weakWords.join(", ")}</div>}
              <button className="arena-primary-button" onClick={() => setPhase("lobby")}><Swords size={17} /> {t("Đấu trận mới", "New match")}</button>
            </div>
          )}

          <div className="arena-insight-bar"><Bot size={17} /><span><strong>LingLing:</strong> {lingLingNote}</span><button onClick={() => setChatOpen(true)}><MessageCircle size={15} /> {t("Hỏi LingLing", "Ask LingLing")}</button></div>
        </section>
      )}

      {chatOpen && <aside className="arena-chat-panel" aria-label="LingLing chatbot">
        <div className="arena-chat-header"><div><Bot size={18} /><strong>LingLing AI</strong><small>{t("Đang phân tích trận đấu", "Analyzing the match")}</small></div><button onClick={() => setChatOpen(false)} aria-label={t("Đóng chat", "Close chat")}><X size={17} /></button></div>
        <div className="arena-chat-messages">{chatMessages.map((message, index) => <div key={`${message.sender}-${index}`} className={`arena-chat-message ${message.sender}`}>{message.text}</div>)}</div>
        <div className="arena-chat-input"><input value={chatInput} onChange={(event) => setChatInput(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") sendChat(); }} placeholder={t("Hỏi về lỗi sai hoặc từ vựng…", "Ask about mistakes or vocabulary…")} /><button onClick={sendChat} aria-label={t("Gửi tin nhắn", "Send message")}><MessageCircle size={16} /></button></div>
      </aside>}
    </main>
  );
}

function PlayerCard({ name, initials, score, progress, isCurrent = false }: { name: string; initials: string; score: number; progress: number; isCurrent?: boolean }) {
  return <div className={`arena-player ${isCurrent ? "is-current" : ""}`}><div className="arena-player-avatar">{initials}</div><div className="arena-player-info"><strong>{name}</strong><small>{isCurrent ? name : `${OPPONENT.level} · 🔥 ${OPPONENT.streak}`}</small></div><strong className="arena-player-score">{score}</strong><div className="arena-player-progress"><span style={{ width: `${Math.min(100, (progress / QUESTIONS.length) * 100)}%` }} /></div></div>;
}

function UsersIcon() {
  return <span className="arena-users-icon" aria-hidden="true">●●●</span>;
}
