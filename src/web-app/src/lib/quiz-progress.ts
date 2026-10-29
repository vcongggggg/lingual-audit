export const QUIZ_PROGRESS_STORAGE_KEY = "lingual.quiz.sessions.v1";

export interface QuizProgressRecord {
  id: string;
  completedAt: string;
  xp: number;
  correctAnswers: number;
  totalQuestions: number;
}

export function readQuizProgress(): QuizProgressRecord[] {
  if (typeof window === "undefined") return [];
  try {
    const value: unknown = JSON.parse(localStorage.getItem(QUIZ_PROGRESS_STORAGE_KEY) ?? "[]");
    if (!Array.isArray(value)) return [];
    return value.filter((record): record is QuizProgressRecord =>
      typeof record?.id === "string" &&
      typeof record?.completedAt === "string" &&
      Number.isFinite(record?.xp) &&
      Number.isFinite(record?.correctAnswers) &&
      Number.isFinite(record?.totalQuestions),
    );
  } catch {
    return [];
  }
}

export function saveQuizProgress(record: QuizProgressRecord): void {
  if (typeof window === "undefined") return;
  const records = readQuizProgress();
  if (records.some((existing) => existing.id === record.id)) return;
  localStorage.setItem(QUIZ_PROGRESS_STORAGE_KEY, JSON.stringify([...records, record]));
  window.dispatchEvent(new Event("lingual:quiz-progress"));
}
