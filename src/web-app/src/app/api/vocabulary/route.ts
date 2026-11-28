import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const params = new URLSearchParams();
  params.set("limit", String(Math.min(6000, Math.max(1, Number(request.nextUrl.searchParams.get("limit") ?? 50)))));
  const level = request.nextUrl.searchParams.get("level");
  if (level) params.set("level", level);
  const query = request.nextUrl.searchParams.get("q")?.trim();
  if (query) params.set("q", query);
  const topic = request.nextUrl.searchParams.get("topic");
  if (topic) params.set("topic", topic);
  const mode = request.nextUrl.searchParams.get("mode");
  if (mode) params.set("mode", mode);
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = {};
    if (authHeader) headers.authorization = authHeader;
    const response = await fetch(`${apiBase}/api/v1/content/vocabulary?${params.toString()}`, {
      headers,
      cache: "no-store",
    });
    if (!response.ok) return NextResponse.json({ error: "Vocabulary database is unavailable" }, { status: response.status });
    return NextResponse.json(await response.json());
  } catch {
    return NextResponse.json({ error: "Could not connect to the vocabulary database" }, { status: 503 });
  }
}
