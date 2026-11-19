import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const level = request.nextUrl.searchParams.get("level");
  if (!level) return NextResponse.json({ error: "A CEFR level is required" }, { status: 400 });

  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const response = await fetch(`${apiBase}/api/v1/content/vocabulary/chapters?level=${encodeURIComponent(level)}`, { cache: "no-store" });
    const body = await response.text();
    let data: unknown = {};
    try { data = body ? JSON.parse(body) : {}; } catch { data = { error: body }; }
    return NextResponse.json(data, { status: response.status });
  } catch {
    return NextResponse.json({ error: "Could not load vocabulary chapters" }, { status: 503 });
  }
}
