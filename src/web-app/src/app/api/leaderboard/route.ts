import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5112";
  const { searchParams } = new URL(request.url);
  const scope = searchParams.get("scope") || "personal";
  const type = searchParams.get("type") || "all-time";
  const limit = searchParams.get("limit") || "20";

  const targetUrl = `${apiBase}/api/v1/content/leaderboard?scope=${encodeURIComponent(scope)}&type=${encodeURIComponent(type)}&limit=${encodeURIComponent(limit)}`;

  try {
    const response = await fetch(targetUrl, { cache: "no-store" });
    if (!response.ok) {
      return NextResponse.json(
        { error: "Failed to fetch leaderboard" },
        { status: response.status }
      );
    }
    return NextResponse.json(await response.json());
  } catch {
    return NextResponse.json({ error: "Could not load leaderboard" }, { status: 503 });
  }
}
