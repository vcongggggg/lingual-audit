import { NextRequest, NextResponse } from "next/server";
import { getBackendBaseUrl } from "@/app/api/api";

const questionCounts = { sprint10: 10, clan20: 20, marathon: 300 } as const;

export async function GET(request: NextRequest) {
  const mode = request.nextUrl.searchParams.get("mode") ?? "sprint10";
  const questionType = request.nextUrl.searchParams.get("questionType");
  const difficulty = request.nextUrl.searchParams.get("difficulty");
  if (!(mode in questionCounts)) {
    return NextResponse.json({ error: "mode must be sprint10, clan20, or marathon" }, { status: 400 });
  }

  const limit = questionCounts[mode as keyof typeof questionCounts];
  const apiBase = getBackendBaseUrl();
  try {
    const params = new URLSearchParams({ limit: String(limit), mode });
    if (questionType) params.set("questionType", questionType);
    if (difficulty) params.set("difficulty", difficulty);
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = {};
    if (authHeader) headers["authorization"] = authHeader;

    const response = await fetch(`${apiBase}/api/v1/content/quizzes?${params.toString()}`, {
      cache: "no-store",
      headers
    });
    if (!response.ok) {
      return NextResponse.json({ error: "Quiz database is unavailable" }, { status: response.status });
    }
    const data = (await response.json()) as { total: number; questions: unknown[] };
    return NextResponse.json({ mode, ...data });
  } catch {
    return NextResponse.json({ error: "Could not connect to the quiz database" }, { status: 503 });
  }
}
