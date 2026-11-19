"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { ArrowLeft, ArrowRight, BookOpen, Check, Clock3, Headphones, Image as ImageIcon, Link2, Mic, Play, RotateCcw, Shuffle, Trophy, Volume2, Zap } from "lucide-react";
import { saveQuizProgress } from "@/lib/quiz-progress";
import LinguaMascot, { type MascotMood } from "@/components/LinguaMascot";
import bank from "@/data/quiz-bank-300.json";
import { mockQuestions, quizTypeOptions, type MockQuestion, type MockQuestionType, type QuizOption } from "@/data/quiz-mock-data";

type Mode = "sprint10" | "clan20" | "marathon";
type Difficulty = "all" | "1" | "2" | "3";
type Question = MockQuestion & { question_type: MockQuestionType };
type Answer = { questionId: string; selectedKey: string | null; isCorrect: boolean };
type Feedback = "correct" | "wrong" | "timeout" | null;
type SpeechRecognitionResultEvent = Event & { results: { [index: number]: { [index: number]: { transcript: string } } } };
type SpeechRecognitionLike = { lang: string; start: () => void; onresult: ((event: SpeechRecognitionResultEvent) => void) | null; onerror: (() => void) | null };
type SpeechRecognitionConstructor = new () => SpeechRecognitionLike;

const modes: { id: Mode; label: string; detail: string; count: number }[] = [
  { id: "sprint10", label: "Sprint", detail: "10 câu · ngắn gọn", count: 10 },
  { id: "clan20", label: "Clan", detail: "20 câu · luyện cùng Clan", count: 20 },
  { id: "marathon", label: "Kho câu hỏi", detail: "300 câu · luyện tập", count: 300 },
];
const difficulties = [{ id: "all", label: "Mọi độ khó" }, { id: "1", label: "Dễ" }, { id: "2", label: "Trung bình" }, { id: "3", label: "Khó" }] as const;
const localBank = bank as unknown as Question[];
const newId = () => typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`;
const normalize = (value: string) => value.trim().toLocaleLowerCase().replace(/[.,!?]/g, "").replace(/\s+/g, " ");
const playFeedbackSound = (feedback: Exclude<Feedback, null>) => {
  if (typeof window === "undefined") return;
  const Constructor = window.AudioContext ?? (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!Constructor) return;
  const context = new Constructor();
  void context.resume().then(() => { const oscillator = context.createOscillator(); oscillator.frequency.value = feedback === "correct" ? 660 : 180; oscillator.connect(context.destination); oscillator.start(); oscillator.stop(context.currentTime + 0.12); window.setTimeout(() => void context.close(), 250); });
};

function speak(question: Question) {
  if (typeof window === "undefined") return;
  const speechText = question.prompt_audio_text ?? question.payload?.expected_text ?? question.prompt;
  const speakFallback = () => {
    window.speechSynthesis?.cancel();
    const utterance = new SpeechSynthesisUtterance(speechText);
    utterance.lang = "en-US";
    window.speechSynthesis?.speak(utterance);
  };
  if (question.prompt_audio_url) {
    const audio = new Audio(question.prompt_audio_url);
    void audio.play().catch(speakFallback);
  } else {
    speakFallback();
  }
}

export default function QuizPage() {
  const [mode, setMode] = useState<Mode>("sprint10");
  const [type, setType] = useState<MockQuestionType>("multiple_choice");
  const [difficulty, setDifficulty] = useState<Difficulty>("all");
  const [questions, setQuestions] = useState<Question[]>([mockQuestions[0]]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [demo, setDemo] = useState(false);
  const [demoMode, setDemoMode] = useState(true);
  const [index, setIndex] = useState(0);
  const [selected, setSelected] = useState<string | null>(null);
  const [textAnswer, setTextAnswer] = useState("");
  const [orderedTokens, setOrderedTokens] = useState<string[]>([]);
  const [matched, setMatched] = useState<Record<string, string>>({});
  const [matchingLeft, setMatchingLeft] = useState<string | null>(null);
  const [answers, setAnswers] = useState<Answer[]>([]);
  const [seconds, setSeconds] = useState(30);
  const [completed, setCompleted] = useState(false);
  const [started, setStarted] = useState(false);
  const [feedback, setFeedback] = useState<Feedback>(null);
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [awardedXp, setAwardedXp] = useState<number | null>(null);
  const [recording, setRecording] = useState(false);
  const sessionId = useRef(newId());
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const current = questions[index];
  const score = answers.filter((answer) => answer.isCorrect).length;
  const selectedType = quizTypeOptions.find((item) => item.id === type);

  const load = useCallback(async (selectedMode: Mode, selectedType: MockQuestionType, selectedDifficulty: Difficulty) => {
    setLoading(true); setError(""); setDemo(false);
    try {
      if (demoMode) throw new Error("demo mode");
      const query = new URLSearchParams({ mode: selectedMode, questionType: selectedType });
      if (selectedDifficulty !== "all") query.set("difficulty", selectedDifficulty);
      const response = await fetch(`/api/quizzes?${query}`, { cache: "no-store" });
      if (!response.ok) throw new Error("server");
      const payload = await response.json() as { questions?: Question[] };
      if (!Array.isArray(payload.questions) || !payload.questions.length) throw new Error("empty");
      setQuestions(payload.questions);
    } catch {
      const count = modes.find((item) => item.id === selectedMode)?.count ?? 10;
      const source = selectedType === "multiple_choice" ? [...mockQuestions.filter((item) => item.question_type === selectedType), ...localBank] : mockQuestions;
      const filtered = source.filter((item) => item.question_type === selectedType && (selectedDifficulty === "all" || item.difficulty === Number(selectedDifficulty)));
      const fallback = filtered.length ? filtered : selectedType === "multiple_choice" ? localBank.filter((item) => item.options?.length === 4) : mockQuestions.filter((item) => item.question_type === selectedType);
      setQuestions(fallback.slice(0, count)); setDemo(true);
      if (!fallback.length) setError("Chưa có mock data cho lựa chọn này.");
    } finally { setLoading(false); }
  }, [demoMode]);

  useEffect(() => { if (!started) { const timer = window.setTimeout(() => void load(mode, type, difficulty), 0); return () => window.clearTimeout(timer); } }, [difficulty, load, mode, started, type]);
  const resetQuestionState = () => { setSelected(null); setTextAnswer(""); setOrderedTokens([]); setMatched({}); setMatchingLeft(null); setFeedback(null); setSeconds(30); };
  const chooseSettings = (nextType: MockQuestionType, nextDifficulty: Difficulty) => {
    if (type === nextType && difficulty === nextDifficulty) return;
    setType(nextType); setDifficulty(nextDifficulty); setQuestions([]); setStarted(false); setCompleted(false); resetQuestionState();
  };
  const start = () => { setStarted(true); setIndex(0); setAnswers([]); setCompleted(false); setSaveState("idle"); setAwardedXp(null); sessionId.current = newId(); setElapsedSeconds(0); resetQuestionState(); };
  const submit = useCallback((value: string | null, correct: boolean) => {
    if (!started || !current || selected !== null || completed) return;
    const result: Exclude<Feedback, null> = value === null ? "timeout" : correct ? "correct" : "wrong";
    setSelected(value ?? "timeout"); setFeedback(result); playFeedbackSound(result);
    setAnswers((previous) => [...previous, { questionId: current.id, selectedKey: value, isCorrect: correct }]);
    setElapsedSeconds((previous) => previous + Math.max(0, 30 - seconds));
  }, [completed, current, seconds, selected, started]);
  const checkAnswer = async (value: string) => {
    if (!current || selected !== null) return;
    if (demo) {
      submit(value, normalize(value) === normalize(current.correct_answer));
      return;
    }
    try {
      const response = await fetch("/api/check-answer", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ questionId: current.id, answer: value }),
      });
      if (response.ok) {
        const data = await response.json() as { isCorrect: boolean; correctAnswer: string; explanation?: string };
        current.correct_answer = data.correctAnswer;
        if (data.explanation) current.explanation = data.explanation;
        submit(value, Boolean(data.isCorrect));
        return;
      }
    } catch {
      // fallback
    }
    submit(value, normalize(value) === normalize(current.correct_answer ?? ""));
  };
  const handleTimeout = async () => {
    if (!current || selected !== null) return;
    if (!demo) {
      try {
        const response = await fetch("/api/check-answer", {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify({ questionId: current.id, answer: "" }),
        });
        if (response.ok) {
          const data = await response.json() as { correctAnswer: string; explanation?: string };
          current.correct_answer = data.correctAnswer;
          if (data.explanation) current.explanation = data.explanation;
        }
      } catch {}
    }
    submit(null, false);
  };
  const finish = useCallback(() => { setElapsedSeconds((value) => value + (selected === null ? Math.max(0, 30 - seconds) : 0)); setCompleted(true); }, [seconds, selected]);
  const next = useCallback(() => { if (selected === null) return; if (index + 1 >= questions.length) finish(); else { setIndex((value) => value + 1); resetQuestionState(); } }, [finish, index, questions.length, selected]);
  useEffect(() => { if (!started || !current || selected !== null || completed) return; if (seconds <= 0) { const timer = window.setTimeout(() => void handleTimeout(), 0); return () => window.clearTimeout(timer); } const timer = window.setTimeout(() => setSeconds((value) => value - 1), 1000); return () => window.clearTimeout(timer); }, [completed, current, seconds, selected, started, handleTimeout]);
  useEffect(() => {
    if (!completed || !questions.length || saveState !== "idle") return;
    const timer = window.setTimeout(() => {
      const earned = score * 10;
      if (demo) {
        setAwardedXp(earned);
        saveQuizProgress({ id: sessionId.current, completedAt: new Date().toISOString(), xp: earned, correctAnswers: score, totalQuestions: questions.length });
        setSaveState("saved");
        return;
      }
      setSaveState("saving");
      const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
      const headers: Record<string, string> = { "content-type": "application/json" };
      if (token) headers["authorization"] = `Bearer ${token}`;

      void fetch("/api/quiz-attempts", { method: "POST", headers, body: JSON.stringify({ idempotencyKey: sessionId.current, answers }), signal: AbortSignal.timeout(10000) }).then(async (response) => {
        if (!response.ok) throw new Error("save failed");
        const result = await response.json() as { xpAwarded?: number; xp?: number };
        const savedXp = Number.isFinite(result.xpAwarded) ? result.xpAwarded! : Number.isFinite(result.xp) ? result.xp! : null;
        setAwardedXp(savedXp);
        if (savedXp !== null) saveQuizProgress({ id: sessionId.current, completedAt: new Date().toISOString(), xp: savedXp, correctAnswers: score, totalQuestions: questions.length });
        setSaveState("saved");
      }).catch(() => setSaveState("failed"));
    }, 0);
    return () => window.clearTimeout(timer);
  }, [answers, completed, demo, questions.length, saveState, score]);

  const optionButtons = (options: QuizOption[]) => <div className="quiz-options">{options.map((option) => { const chosen = selected === option.key; const right = selected !== null && option.key === current.correct_answer; const imageOnly = current.question_type === "image_choice" && option.image_url; return <button key={option.key} disabled={selected !== null} onClick={() => checkAnswer(option.key)} className={`${chosen ? "is-chosen" : ""} ${right ? "is-right" : ""} ${chosen && !right ? "is-wrong" : ""}`}><span className="quiz-option-key">{option.key}</span>{option.image_url ? <img src={option.image_url} alt={imageOnly ? "" : option.text} width={120} height={70} /> : null}{!imageOnly && <span>{option.text}</span>}{right && <Check size={18}/>}</button>; })}</div>;
  const renderQuestion = () => {
    if (current.question_type === "dictation") return <div className="quiz-answer-form quiz-answer-form-dictation"><div className="quiz-task-icon"><Headphones size={22}/><span>Nghe thật kỹ</span></div><button className="quiz-secondary quiz-audio-button" onClick={() => speak(current)}><Volume2 size={17}/> Phát audio</button><input value={textAnswer} onChange={(event) => setTextAnswer(event.target.value)} placeholder="Nhập từ hoặc câu bạn nghe được" disabled={selected !== null} onKeyDown={(event) => { if (event.key === "Enter") checkAnswer(textAnswer); }} /><button className="quiz-primary" disabled={!textAnswer.trim() || selected !== null} onClick={() => checkAnswer(textAnswer)}>Kiểm tra</button></div>;
    if (current.question_type === "word_matching") {
      const pairs = current.payload?.pairs ?? []; const left = pairs.map((pair) => pair.left); const right = pairs.map((pair) => pair.right);
      const rightIndex = (meaning: string) => right.indexOf(meaning);
      const selectLeft = (item: string) => setMatchingLeft((value) => value === item ? null : item);
      const selectRight = (meaning: string) => {
        if (!matchingLeft) return;
        setMatched((value) => ({ ...value, [matchingLeft]: meaning }));
        setMatchingLeft(null);
      };
      return <div className="quiz-matching"><div className="quiz-task-icon"><Link2 size={22}/><span>Bấm từ rồi bấm nghĩa để nối dây</span></div><div className="quiz-match-board"><svg className="quiz-match-lines" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">{left.map((item, itemIndex) => { const meaning = matched[item]; if (!meaning) return null; return <line key={item} x1="25" y1={`${((itemIndex + .5) / left.length) * 100}`} x2="75" y2={`${((rightIndex(meaning) + .5) / right.length) * 100}`} />; })}</svg><div className="quiz-match-column">{left.map((item) => <button key={item} disabled={selected !== null} className={`${matched[item] ? "is-matched" : ""} ${matchingLeft === item ? "is-linking" : ""}`} onClick={() => selectLeft(item)}><span className="quiz-match-dot" />{item}</button>)}</div><div className="quiz-match-column">{right.map((meaning) => <button key={meaning} disabled={selected !== null || Object.values(matched).includes(meaning)} className={Object.values(matched).includes(meaning) ? "is-matched" : ""} onClick={() => selectRight(meaning)}><span className="quiz-match-dot" />{meaning}</button>)}</div></div><button className="quiz-primary" disabled={Object.keys(matched).length !== pairs.length || selected !== null} onClick={() => checkAnswer(left.map((item, itemIndex) => `${itemIndex}:${rightIndex(matched[item])}`).join(","))}>Kiểm tra cặp nối</button></div>;
    }
    if (current.question_type === "sentence_scramble") return <div className="quiz-scramble"><div className="quiz-task-icon"><Shuffle size={22}/><span>Xây câu từng bước</span></div><div className="quiz-token-tray">{(current.payload?.tokens ?? current.options.map((option) => option.text)).map((token) => <button key={token} disabled={selected !== null || orderedTokens.includes(token)} onClick={() => setOrderedTokens((value) => [...value, token])}>{token}</button>)}</div><p className="quiz-sentence">{orderedTokens.join(" ") || "Chạm các từ theo đúng thứ tự"}</p>{orderedTokens.length > 0 && selected === null ? <button className="quiz-undo" onClick={() => setOrderedTokens((value) => value.slice(0, -1))}><RotateCcw size={14}/> Hoàn tác từ cuối</button> : null}<button className="quiz-primary" disabled={!orderedTokens.length || selected !== null} onClick={() => checkAnswer(orderedTokens.join(" "))}>Kiểm tra câu</button></div>;
    if (current.question_type === "pronunciation") return <div className="quiz-answer-form quiz-answer-form-pronunciation"><div className={`quiz-mic-visual ${recording ? "is-recording" : ""}`}><Mic size={28}/><span>{recording ? "Đang lắng nghe…" : "Sẵn sàng nghe bạn"}</span></div><button className={`quiz-primary ${recording ? "is-urgent" : ""}`} disabled={selected !== null} onClick={() => { const Recognition = (window as Window & { SpeechRecognition?: SpeechRecognitionConstructor; webkitSpeechRecognition?: SpeechRecognitionConstructor }).SpeechRecognition ?? (window as Window & { webkitSpeechRecognition?: SpeechRecognitionConstructor }).webkitSpeechRecognition; if (!Recognition) { setTextAnswer(current.payload?.expected_text ?? ""); return; } const recognition = new Recognition(); recognition.lang = "en-US"; recognition.onresult = (event) => { const transcript = event.results[0][0].transcript; setTextAnswer(transcript); setRecording(false); submit(transcript, normalize(transcript) === normalize(current.correct_answer)); }; recognition.onerror = () => setRecording(false); setRecording(true); recognition.start(); }}><Mic size={16}/> {recording ? "Đang nghe…" : "Bấm để đọc"}</button><p className="quiz-sentence">{textAnswer || "Phát âm câu mẫu bằng microphone"}</p>{!recording && textAnswer && selected === null ? <button className="quiz-secondary" onClick={() => submit(textAnswer, normalize(textAnswer) === normalize(current.correct_answer))}>Kiểm tra phát âm</button> : null}</div>;
    if (current.question_type === "listening_choice" || current.question_type === "spelling" || current.question_type === "image_choice") return <div className={`quiz-choice-task quiz-choice-task-${current.question_type}`}><div className="quiz-task-icon">{current.question_type === "image_choice" ? <ImageIcon size={22}/> : <Headphones size={22}/>}<span>{current.question_type === "image_choice" ? "Nhìn và chọn hình phù hợp" : "Nghe và chọn đáp án"}</span></div><button className="quiz-secondary quiz-audio-button" onClick={() => speak(current)}><Play size={16}/> Phát audio</button>{optionButtons(current.options)}</div>;
    return optionButtons(current.options);
  };
  const mascotMood: MascotMood = completed ? "complete" : selected === null ? (started ? "thinking" : "idle") : selected === current?.correct_answer ? "correct" : "encourage";

  return <main className="quiz-page"><header className="quiz-heading"><div><p className="quiz-eyebrow">LINGUAL · LUYỆN TẬP</p><h1>Quiz đa dạng kỹ năng</h1><p>Chọn loại bài và độ khó trước khi bắt đầu.</p></div><div className="quiz-heading-actions"><label className="demo-toggle"><input type="checkbox" checked={demoMode} onChange={(event) => { setDemoMode(event.target.checked); setStarted(false); }} /><span /> Mock data</label><Link href="/leaderboard" className="quiz-back"><ArrowLeft size={16}/> Bảng xếp hạng</Link></div></header>
    {!started && <section className="quiz-start"><div className="quiz-start-visual"><LinguaMascot mood="idle" /><div><p className="quiz-eyebrow">THIẾT LẬP PHIÊN HỌC</p><h2>{selectedType?.label}</h2><p>{selectedType?.description}. {loading ? "Đang tải dữ liệu…" : "Dữ liệu thật sẽ được dùng khi máy chủ sẵn sàng; mock data dùng để xem trước."}</p></div></div><div className="quiz-start-modes" aria-label="Chọn loại quiz">{quizTypeOptions.map((item) => <button key={item.id} onClick={() => chooseSettings(item.id, difficulty)} className={type === item.id ? "is-active" : ""} aria-pressed={type === item.id}><strong>{item.label}</strong><span>{item.description}</span></button>)}</div><div className="quiz-start-modes" aria-label="Chọn độ khó">{difficulties.map((item) => <button key={item.id} onClick={() => chooseSettings(type, item.id)} className={difficulty === item.id ? "is-active" : ""} aria-pressed={difficulty === item.id}><strong>{item.label}</strong></button>)}</div><div className="quiz-start-modes" aria-label="Chọn nhịp học">{modes.map((item) => <button key={item.id} onClick={() => setMode(item.id)} className={mode === item.id ? "is-active" : ""}><strong>{item.label}</strong><span>{item.detail}</span></button>)}</div><button className="quiz-primary quiz-start-button" disabled={loading || !questions.length} onClick={start}><Zap size={16}/> Bắt đầu phiên học <ArrowRight size={16}/></button>{error && <p className="quiz-demo-banner">{error}</p>}</section>}
    {started && current && !completed && <><nav className="quiz-modes" aria-label="Chọn chế độ quiz">{modes.map((item) => <button key={item.id} onClick={() => { setMode(item.id); setStarted(false); }} className={mode === item.id ? "is-active" : ""}><strong>{item.label}</strong><span>{item.detail}</span></button>)}</nav>{demo && <p className="quiz-demo-banner">Mock data · Kết quả chỉ dùng để xem trước giao diện.</p>}<section className="quiz-session"><div className="quiz-progress-top"><span>Câu <strong>{index + 1}</strong> / {questions.length}</span><div className="quiz-progress-track"><span style={{ width: `${((index + 1) / questions.length) * 100}%` }}/></div></div><article className={`quiz-question ${feedback ? `has-feedback feedback-${feedback}` : ""}`}><div className="quiz-question-mascot"><LinguaMascot mood={mascotMood} compact /></div><div className="quiz-question-meta"><span>{current.level} · Độ khó {current.difficulty}/3</span><span className={seconds <= 5 && selected === null ? "is-urgent" : ""}><Clock3 size={15}/>{selected === null ? `${seconds} giây` : "Đã trả lời"}</span></div><h2>{current.prompt}</h2>{renderQuestion()}{selected !== null && <div className={`quiz-feedback feedback-${feedback}`} role="status"><span>{feedback === "correct" ? "✨" : feedback === "timeout" ? "⏰" : "💡"}</span><strong>{feedback === "correct" ? "Chính xác!" : feedback === "timeout" ? "Đã hết giờ" : `Đáp án: ${current.correct_answer}`}</strong><p>{current.explanation}</p></div>}</article><div className="quiz-controls"><span>Hoàn thành thao tác để tiếp tục</span><div className="quiz-control-actions"><button className="quiz-end-button" onClick={finish}>Kết thúc bài</button>{selected !== null && <button className="quiz-primary" onClick={next}>{index + 1 === questions.length ? "Xem kết quả" : "Câu tiếp theo"}<ArrowRight size={16}/></button>}</div></div></section></>}
    {started && completed && <section className="quiz-results"><LinguaMascot mood="complete" /><span className="quiz-result-icon"><Trophy size={30}/></span><p className="quiz-eyebrow">HOÀN THÀNH PHIÊN HỌC</p><h2>{score >= questions.length / 2 ? "Bạn đang tiến bộ!" : "Mỗi lần luyện tập đều có ích."}</h2><div className="quiz-result-stats"><div><span>Câu đúng</span><strong>{score}<small> / {questions.length}</small></strong></div><div><span>Độ chính xác</span><strong>{Math.round(score / questions.length * 100)}%</strong></div><div><span>XP đã nhận</span><strong>{awardedXp ?? "—"}</strong></div><div><span>Thời gian</span><strong>{Math.floor(elapsedSeconds / 60)}<small> phút</small></strong></div></div><p className="quiz-save-state">{saveState === "failed" ? "Không thể lưu kết quả." : saveState === "saved" ? "Kết quả đã được lưu." : "Đang gửi kết quả…"}</p><div className="quiz-result-actions"><button className="quiz-primary" onClick={() => { setStarted(false); setCompleted(false); resetQuestionState(); }}><RotateCcw size={16}/> Chọn bài khác</button><Link className="quiz-secondary" href="/"><BookOpen size={16}/> Về trang chủ</Link></div></section>}</main>;
}
