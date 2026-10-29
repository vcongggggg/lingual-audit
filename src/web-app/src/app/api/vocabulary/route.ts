import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const limit = Math.min(500, Math.max(1, Number(request.nextUrl.searchParams.get("limit") ?? 50)));
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";

  try {
    const response = await fetch(`${apiBase}/api/v1/content/vocabulary?limit=${limit}`, { cache: "no-store" });
    if (!response.ok) {
      return NextResponse.json({ error: "Vocabulary database is unavailable" }, { status: response.status });
    }
    return NextResponse.json(await response.json());
  } catch {
    return NextResponse.json({ error: "Could not connect to the vocabulary database" }, { status: 503 });
  }
}
