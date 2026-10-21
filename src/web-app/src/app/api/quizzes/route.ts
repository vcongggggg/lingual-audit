import { NextRequest, NextResponse } from "next/server";
import quizBank from "@/data/quiz-bank-300.json";

const questionCounts = { sprint10: 10, clan20: 20, marathon: 300 } as const;

export function GET(request: NextRequest) {
  const mode = request.nextUrl.searchParams.get("mode") ?? "sprint10";
  if (!(mode in questionCounts)) {
    return NextResponse.json({ error: "mode must be sprint10, clan20, or marathon" }, { status: 400 });
  }

  const limit = questionCounts[mode as keyof typeof questionCounts];
  const questions = quizBank.slice(0, limit);
  return NextResponse.json({ mode, total: questions.length, questions });
}
