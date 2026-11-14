"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { ArrowRight, BookOpen, Headphones, Search, Volume2 } from "lucide-react";
import type { VocabularyItem } from "@/components/FlashcardStudy";
import { MOCK_VOCABULARY } from "@/lib/mock-data";
import { useLanguage } from "@/lib/language-context";

type Payload = { words?: VocabularyItem[] };

export default function WordsPage() {
  const { translate: t } = useLanguage();
  const [words, setWords] = useState<VocabularyItem[]>([]);
  const [search, setSearch] = useState("");
  const [level, setLevel] = useState("all");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [demo, setDemo] = useState(false);
  const [speakingId, setSpeakingId] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/vocabulary?limit=500", { signal: controller.signal, cache: "no-store" })
      .then((response) => { if (!response.ok) throw new Error("Vocabulary unavailable"); return response.json() as Promise<Payload>; })
      .then((payload) => { if (!Array.isArray(payload.words) || payload.words.length === 0) throw new Error("Vocabulary empty"); setWords(payload.words); setError(false); setDemo(false); })
      .catch(() => { if (!controller.signal.aborted) { setWords(MOCK_VOCABULARY); setDemo(true); setError(false); } })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, []);

  const levels = useMemo(() => Array.from(new Set(words.map((word) => word.cefr_level).filter(Boolean))).sort(), [words]);
  const filtered = useMemo(() => words.filter((word) => {
    const query = search.trim().toLocaleLowerCase();
    const matchesQuery = !query || [word.term, word.meaning, word.ipa ?? ""].some((value) => value.toLocaleLowerCase().includes(query));
    return matchesQuery && (level === "all" || word.cefr_level === level);
  }), [words, search, level]);
  const pronounce = (word: VocabularyItem) => {
    if (!("speechSynthesis" in window)) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(word.term);
    utterance.lang = "en-US"; utterance.rate = 0.88;
    utterance.onstart = () => setSpeakingId(word.id);
    utterance.onend = () => setSpeakingId(null);
    utterance.onerror = () => setSpeakingId(null);
    window.speechSynthesis.speak(utterance);
  };

  return <main className="words-page">
    <header className="words-heading"><div><p className="dashboard-eyebrow"><BookOpen size={14}/> MY WORDS</p><h1>{t("Thư viện từ vựng", "Vocabulary library")}</h1><p>{t("Tìm kiếm từ, xem nghĩa và ví dụ trong kho từ đang kết nối.", "Search words, meanings, and examples in the connected library.")}</p></div><Link href="/learn" className="dashboard-primary">{t("Ôn flashcard", "Review flashcards")} <ArrowRight size={16}/></Link></header>
    <section className="words-tools" aria-label={t("Tìm và lọc từ vựng", "Search and filter vocabulary")}><label className="words-search"><Search size={17}/><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={t("Tìm từ, nghĩa hoặc IPA", "Search word, meaning, or IPA")} aria-label={t("Tìm từ, nghĩa hoặc IPA", "Search word, meaning, or IPA")}/></label><div className="words-level-filter" aria-label={t("Lọc theo CEFR", "Filter by CEFR")}><button className={level === "all" ? "is-active" : ""} onClick={() => setLevel("all")}>{t("Tất cả", "All")}</button>{levels.map((item) => <button key={item} className={level === item ? "is-active" : ""} onClick={() => setLevel(item ?? "all")}>{item}</button>)}</div><span className="words-count">{loading ? t("Đang tải…", "Loading…") : `${filtered.length} ${t("từ", "words")}`}</span></section>
    {error ? <section className="words-empty" role="alert"><BookOpen size={24}/><h2>{t("Chưa kết nối được thư viện", "Library unavailable")}</h2><p>{t("Kiểm tra kết nối máy chủ từ vựng rồi thử tải lại.", "Check the vocabulary server connection and try again.")}</p><button className="quiz-secondary" onClick={() => window.location.reload()}>{t("Thử tải lại", "Try again")}</button></section> : loading ? <section className="words-empty" role="status">{t("Đang tải từ vựng…", "Loading vocabulary…")}</section> : filtered.length === 0 ? <section className="words-empty"><Search size={24}/><h2>{t("Không tìm thấy từ phù hợp", "No matching words")}</h2><p>{t("Thử một từ khóa khác hoặc đổi cấp độ CEFR.", "Try another keyword or CEFR level.")}</p></section> : <section className="words-grid" aria-label={t("Danh sách từ vựng", "Vocabulary list")}>{filtered.map((word) => <article className="word-card" key={word.id}><div className="word-card-top">{word.cefr_level && <span className="study-level">{word.cefr_level}</span>}<span>{word.part_of_speech}</span><button className={`word-audio ${speakingId === word.id ? "is-speaking" : ""}`} onClick={() => pronounce(word)} aria-label={`${t("Nghe phát âm", "Play pronunciation")} ${word.term}`}><Volume2 size={16}/></button></div><h2>{word.term}</h2>{word.ipa && <p className="word-ipa">{word.ipa}</p>}<p className="word-meaning">{word.meaning}</p>{word.examples?.[0] && <div className="word-example"><p lang="en">“{word.examples[0].source_text}”</p>{word.examples[0].target_text && <small>{word.examples[0].target_text}</small>}</div>}</article>)}</section>}
    <p className={`words-note ${demo ? "is-demo" : ""}`}><Headphones size={14}/> {demo ? t("Bản demo · Đang hiển thị 8 từ mẫu vì máy chủ chưa có dữ liệu.", "Demo · Showing 8 sample words because the server has no data.") : t("Số từ và nội dung hiển thị được tải từ thư viện hiện có.", "Words and content are loaded from the available library.")} {t("Trạng thái ghi nhớ từng từ chưa được API cung cấp.", "Per-word memory status is not provided by the API.")}</p>
  </main>;
}
