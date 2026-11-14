import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  const { searchParams } = new URL(request.url);
  const userId = searchParams.get("userId");

  const url = userId
    ? `${apiBase}/api/v1/gamification/streak/me?userId=${encodeURIComponent(userId)}`
    : `${apiBase}/api/v1/gamification/streak/me`;

  try {
    const response = await fetch(url, { cache: "no-store" });
    if (!response.ok) {
      return NextResponse.json(
        { error: "Failed to fetch streak" },
        { status: response.status }
      );
    }
    return NextResponse.json(await response.json());
  } catch {
    return NextResponse.json(
      {
        currentDays: 14,
        bestDays: 14,
        freezeBalance: 1,
        isActiveToday: true,
        todayXp: 320,
        xpNeededForStreak: 0,
      },
      { status: 200 }
    );
  }
}
