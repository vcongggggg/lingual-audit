import { NextResponse } from "next/server";

export async function GET() {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const response = await fetch(`${apiBase}/api/v1/content/leaderboard`, { cache: "no-store" });
    return NextResponse.json(await response.json(), { status: response.status });
  } catch {
    return NextResponse.json({ error: "Could not load leaderboard" }, { status: 503 });
  }
}
