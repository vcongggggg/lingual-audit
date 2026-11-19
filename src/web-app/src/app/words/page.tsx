"use client";

import { useCallback, useEffect, useState } from "react";
import { ArrowLeft, ArrowRight, BookOpen, Search } from "lucide-react";
import FlashcardStudy, { type SrsReviewPayload, type VocabularyItem } from "@/components/FlashcardStudy";
import { useLanguage } from "@/lib/language-context";

type LevelGroup = { level: string; title: string; description: string; target_words: number; word_count: number; chapters: number };
type GroupsPayload = { groups?: LevelGroup[] };
type WordsPayload = { words?: VocabularyItem[] };
type VocabularyChapter = { id: string; title: string; description: string; word_count: number };
type ChaptersPayload = { chapters?: VocabularyChapter[] };
type ReviewProgress = { learnedWords?: VocabularyItem[]; dueWords?: VocabularyItem[]; totalLearned?: number; totalDue?: number };

async function fetchJsonWithRetry<T>(url: string, signal?: AbortSignal): Promise<T> {
  let lastError: unknown;
  for (let attempt = 0; attempt < 4; attempt += 1) {
    if (signal?.aborted) throw new DOMException("Request aborted", "AbortError");
    try {
      const response = await fetch(url, { signal, cache: "no-store" });
      if (!response.ok) throw new Error(`Request failed (${response.status})`);
      return await response.json() as T;
    } catch (error) {
      if (signal?.aborted) throw error;
      lastError = error;
      if (attempt < 3) await new Promise((resolve) => setTimeout(resolve, 400 * (2 ** attempt)));
    }
  }
  throw lastError ?? new Error("Request failed");
}

export default function WordsPage() {
  const { translate: t } = useLanguage();
  const [groups, setGroups] = useState<LevelGroup[]>([]);
  const [words, setWords] = useState<VocabularyItem[]>([]);
  const [search, setSearch] = useState("");
  const [searchResults, setSearchResults] = useState<VocabularyItem[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [searchError, setSearchError] = useState(false);
  const [selectedLevel, setSelectedLevel] = useState<LevelGroup | null>(null);
  const [chapters, setChapters] = useState<VocabularyChapter[]>([]);
  const [chaptersLoading, setChaptersLoading] = useState(false);
  const [deckTitle, setDeckTitle] = useState("");
  const [progress, setProgress] = useState<ReviewProgress>({});
  const [progressLoading, setProgressLoading] = useState(true);
  const [progressError, setProgressError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [groupLoading, setGroupLoading] = useState(false);
  const [error, setError] = useState(false);

  const loadProgress = useCallback(async () => {
    const token = localStorage.getItem("accessToken");
    if (!token) {
      setProgress({});
      setProgressError(true);
      setProgressLoading(false);
      return;
    }
    setProgressLoading(true);
    try {
      const response = await fetch("/api/reviews", { headers: { authorization: `Bearer ${token}` }, cache: "no-store" });
      if (!response.ok) throw new Error(`Progress request failed (${response.status})`);
      setProgress(await response.json() as ReviewProgress);
      setProgressError(false);
    } catch {
      setProgressError(true);
    } finally {
      setProgressLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void fetchJsonWithRetry<GroupsPayload>("/api/vocabulary/groups", controller.signal)
      .then((data) => { setGroups(data.groups ?? []); setError(false); })
      .catch(() => { if (!controller.signal.aborted) setError(true); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    void loadProgress();
    return () => controller.abort();
  }, [loadProgress]);

  useEffect(() => {
    const query = search.trim();
    if (!query) {
      setSearchResults([]);
      setSearchLoading(false);
      setSearchError(false);
      return;
    }
    const controller = new AbortController();
    setSearchResults([]);
    setSearchLoading(true);
    setSearchError(false);
    const debounce = window.setTimeout(() => {
      void fetchJsonWithRetry<WordsPayload>(`/api/vocabulary?limit=100&q=${encodeURIComponent(query)}`, controller.signal)
        .then((data) => setSearchResults(data.words ?? []))
        .catch(() => { if (!controller.signal.aborted) setSearchError(true); })
        .finally(() => { if (!controller.signal.aborted) setSearchLoading(false); });
    }, 250);
    return () => { window.clearTimeout(debounce); controller.abort(); };
  }, [search]);

  const startDeck = useCallback((title: string, deckWords: VocabularyItem[]) => {
    setDeckTitle(title);
    setSelectedLevel(null);
    setWords(deckWords);
    setGroupLoading(false);
    setSearch("");
  }, []);

  const chooseGroup = useCallback(async (group: LevelGroup) => {
    setSelectedLevel(group);
    setChapters([]);
    setChaptersLoading(true);
    try {
      const data = await fetchJsonWithRetry<ChaptersPayload>(`/api/vocabulary/chapters?level=${encodeURIComponent(group.level)}`);
      setChapters(data.chapters ?? []);
    } catch {
      setChapters([]);
    } finally {
      setChaptersLoading(false);
    }
  }, []);

  const chooseChapter = useCallback(async (chapter: VocabularyChapter) => {
    if (!selectedLevel) return;
    setDeckTitle(`${selectedLevel.level} · ${chapter.title}`);
    setGroupLoading(true);
    try {
      const data = await fetchJsonWithRetry<WordsPayload>(`/api/vocabulary?limit=6000&level=${encodeURIComponent(selectedLevel.level)}&topic=${encodeURIComponent(chapter.id)}`);
      setWords(data.words ?? []);
    } catch {
      setWords([]);
    } finally {
      setGroupLoading(false);
    }
  }, [selectedLevel]);

  const openSearchResult = useCallback((word: VocabularyItem) => {
    startDeck(word.term, [word]);
  }, [startDeck]);

  const saveReview = useCallback(async (payload: SrsReviewPayload) => {
    const token = localStorage.getItem("accessToken");
    if (!token) throw new Error("Sign in to save flashcard progress.");
    const response = await fetch("/api/reviews", {
      method: "POST",
      headers: { "content-type": "application/json", authorization: `Bearer ${token}` },
      body: JSON.stringify({
        vocabularyId: payload.vocabulary_id,
        rating: payload.rating,
        quality: payload.quality,
        repetitions: payload.repetitions,
        intervalDays: payload.interval_days,
        easeFactor: payload.ease_factor,
        nextDueAt: payload.next_due_at,
        timeSpentMs: payload.time_spent_ms,
        idempotencyKey: payload.idempotency_key,
      }),
    });
    if (!response.ok) throw new Error(`Failed to save review (HTTP ${response.status})`);
  }, []);

  const matchingGroups = groups.filter((group) => [group.level, group.title, group.description].some((value) => value.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase())));
  const inStudy = Boolean(deckTitle);

  return <main className="words-page words-level-page">
    {inStudy ? <>
      <header className="vocab-study-heading">
        <button className="vocab-back" onClick={() => { setDeckTitle(""); if (!selectedLevel) void loadProgress(); }}><ArrowLeft size={17}/>{selectedLevel ? t("Các nhóm từ", "Vocabulary groups") : t("Tất cả nhóm", "All groups")}</button>
        <div><span className="vocab-kicker">{t("FLASHCARD · NHÓM TỪ VỰNG", "FLASHCARDS · VOCABULARY GROUP")}</span><h1>{deckTitle}</h1></div>
      </header>
      {groupLoading ? <section className="words-empty" role="status">{t("Đang tải bộ từ…", "Loading vocabulary deck…")}</section> : words.length ? <FlashcardStudy key={deckTitle} vocabularyList={words} onReviewSubmit={saveReview} /> : <section className="words-empty"><BookOpen size={24}/><h2>{t("Nhóm này chưa có từ", "No words in this group yet")}</h2><p>{t("Hãy chọn nhóm từ vựng khác.", "Choose another vocabulary group.")}</p></section>}
    </> : selectedLevel ? <>
      <header className="vocab-study-heading">
        <button className="vocab-back" onClick={() => { setSelectedLevel(null); setChapters([]); }}><ArrowLeft size={17}/>{t("Tất cả chương", "All chapters")}</button>
        <div><span className="vocab-kicker">{t("CHƯƠNG", "CHAPTER")} · {selectedLevel.level}</span><h1>{selectedLevel.title}</h1><p>{t("Chọn nhóm từ vựng theo chủ đề để bắt đầu flashcard.", "Choose a topic group to start its flashcards.")}</p></div>
      </header>
      {chaptersLoading ? <section className="words-empty" role="status">{t("Đang tải nhóm từ…", "Loading topic groups…")}</section> : chapters.length ? <section className="vocab-level-grid" aria-label={t("Nhóm từ vựng", "Vocabulary topic groups")}>{chapters.map((chapter, index) => <button className="vocab-level-card vocab-topic-card" key={chapter.id} onClick={() => void chooseChapter(chapter)}><span className="vocab-chapter">{t("NHÓM", "GROUP")} {String(index + 1).padStart(2, "0")}</span><span className="vocab-level-mark">{chapter.word_count}</span><h2>{chapter.title}</h2><div className="vocab-card-rule"/><p className="vocab-level-description">{chapter.description}</p><span className="vocab-card-action"><b>{selectedLevel.level}</b><strong>{t("Ôn nhóm này", "Study this group")} <ArrowRight size={15}/></strong></span></button>)}</section> : <section className="words-empty"><BookOpen size={24}/><h2>{t("Chưa có nhóm từ cho chương này", "No topic groups in this chapter")}</h2></section>}
    </> : <>
      <header className="vocab-library-heading"><div><span className="vocab-kicker">{t("HỌC TỪ VỰNG THEO LỘ TRÌNH", "LEARN BY LEVEL")}</span><h1>{t("Thư viện từ vựng", "Vocabulary library")}</h1><p>{t("Chọn một nhóm để học bằng flashcard; tiến độ được lưu vào tài khoản của bạn.", "Choose a group to study with flashcards. Your progress is saved to your account.")}</p></div></header>
      <nav className="vocab-tabs" aria-label={t("Danh mục từ vựng", "Vocabulary categories")}>
        <span className="is-current">{t("Theo bậc CEFR (A1–C2)", "CEFR levels (A1–C2)")}</span><span>{t("Chứng chỉ (IELTS / TOEIC)", "Certificates (IELTS / TOEIC)")}</span><span>{t("Chủ đề thông dụng", "Common topics")}</span><span>{t("Từ nâng cao", "Advanced vocabulary")}</span>
      </nav>
      <section className="vocab-progress-section" aria-label={t("Tiến độ học", "Study progress")}>
        <button className="vocab-progress-card" onClick={() => startDeck(t("Từ đã học", "Learned words"), progress.learnedWords ?? [])} disabled={progressLoading || !(progress.learnedWords?.length)}><BookOpen size={18}/><span><b>{t("Từ đã học", "Learned words")}</b><small>{progressLoading ? t("Đang tải…", "Loading…") : `${progress.totalLearned ?? 0} ${t("từ đã lưu", "words saved")}`}</small></span><ArrowRight size={16}/></button>
        <button className="vocab-progress-card is-due" onClick={() => startDeck(t("Từ cần ôn", "Due for review"), progress.dueWords ?? [])} disabled={progressLoading || !(progress.dueWords?.length)}><ArrowRight size={18}/><span><b>{t("Từ cần ôn", "Due for review")}</b><small>{progressLoading ? t("Đang tải…", "Loading…") : `${progress.totalDue ?? 0} ${t("thẻ đến hạn", "cards due")}`}</small></span><ArrowRight size={16}/></button>
      </section>
      {progressError && <p className="vocab-search-message">{t("Đăng nhập để lưu flashcard và xem danh sách từ đã học, từ cần ôn.", "Sign in to save flashcards and view learned or due words.")}</p>}
      <label className="vocab-search"><Search size={17}/><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder={t("Tìm theo cấp độ, từ vựng, tên chủ đề…", "Search levels, words, or topics…")}/></label>
      {error ? <section className="words-empty" role="alert"><h2>{t("Chưa kết nối được thư viện", "Library unavailable")}</h2><button className="quiz-secondary" onClick={() => window.location.reload()}>{t("Thử tải lại", "Try again")}</button></section> : loading ? <section className="words-empty" role="status">{t("Đang tải cấp độ…", "Loading levels…")}</section> : search.trim() ? <section className="vocab-search-results" aria-live="polite">
        {matchingGroups.length > 0 && <div className="vocab-search-levels"><h2>{t("Cấp độ phù hợp", "Matching levels")}</h2>{matchingGroups.map((group) => <button key={group.level} onClick={() => void chooseGroup(group)}><b>{group.level}</b><span>{group.title}</span><ArrowRight size={14}/></button>)}</div>}
        <div className="vocab-results-heading"><h2>{t("Từ vựng phù hợp", "Matching vocabulary")}</h2><span>{searchLoading ? t("Đang tìm…", "Searching…") : searchError ? t("Không tìm được dữ liệu", "Search unavailable") : `${searchResults.length} ${t("kết quả", "results")}`}</span></div>
        {searchLoading && <p className="vocab-search-message" role="status">{t("Đang tìm trong thư viện…", "Searching the vocabulary library…")}</p>}
        {searchError && <p className="vocab-search-message" role="alert">{t("Không tải được kết quả. Hãy thử lại sau.", "Could not load results. Please try again.")}</p>}
        {!searchLoading && !searchError && searchResults.length === 0 && matchingGroups.length === 0 && <p className="vocab-search-message">{t("Không tìm thấy từ phù hợp. Thử từ khóa khác nhé.", "No matches found. Try another search.")}</p>}
        {searchResults.length > 0 && <div className="vocab-word-results">{searchResults.map((word) => <button className="vocab-word-result" key={word.id} onClick={() => openSearchResult(word)}><span className="vocab-result-top"><strong>{word.term}</strong>{word.ipa && <small>{word.ipa}</small>}<b>{word.cefr_level}</b></span><span className="vocab-result-meaning">{word.meaning}</span>{word.examples?.[0]?.source_text && <span className="vocab-result-example">{word.examples[0].source_text}</span>}</button>)}</div>}
      </section> : groups.length === 0 ? <section className="words-empty"><h2>{t("Chưa có nhóm từ", "No vocabulary groups")}</h2><p>{t("Chưa có cấp độ từ vựng nào.", "There are no vocabulary levels yet.")}</p></section> : <section className="vocab-level-grid" aria-label={t("Các cấp độ từ vựng", "Vocabulary levels")}>{groups.map((group, index) => <button className="vocab-level-card" key={group.level} onClick={() => void chooseGroup(group)}><span className="vocab-chapter">{t("CHƯƠNG", "CHAPTER")} {String(index + 1).padStart(2, "0")}</span><span className="vocab-level-mark">{group.level}</span><h2>{group.title}</h2><div className="vocab-card-rule"/><div className="vocab-card-preview"><div><b>{group.target_words.toLocaleString()} {t("từ cốt lõi", "core words")}</b><span>{group.chapters} {t("bài học", "lessons")}</span></div><div className="vocab-tags"><i>{group.level === "A1" ? "greetings" : group.level === "A2" ? "daily life" : group.level === "B1" ? "work" : group.level === "B2" ? "technology" : group.level === "C1" ? "nuance" : "idioms"}</i><i>{group.level === "A1" ? "family" : group.level === "A2" ? "travel" : group.level === "B1" ? "education" : group.level === "B2" ? "research" : group.level === "C1" ? "collocations" : "advanced"}</i><i>{group.word_count} {t("từ có sẵn", "available")}</i></div></div><p className="vocab-level-description">{group.description}</p><span className="vocab-card-action"><b>{group.level}</b><strong>{t("Vào học ngay", "Study now")} <ArrowRight size={15}/></strong></span></button>)}</section>}
    </>}
  </main>;
}
