import { NextRequest, NextResponse } from "next/server";
import { proxyBackend } from "@/app/api/api";

const questionCounts = { sprint10: 10, clan20: 20, marathon: 300 } as const;

export async function GET(request: NextRequest) {
  const mode = request.nextUrl.searchParams.get("mode") ?? "sprint10";
  if (!(mode in questionCounts)) {
    return NextResponse.json({ error: "mode must be sprint10, clan20, or marathon" }, { status: 400 });
  }

  const limit = questionCounts[mode as keyof typeof questionCounts];
  return proxyBackend(request, `/api/v1/content/quizzes?limit=${limit}`);
}
