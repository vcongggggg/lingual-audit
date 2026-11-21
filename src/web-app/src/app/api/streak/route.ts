import { NextRequest, NextResponse } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const userId = searchParams.get("userId");

  const targetPath = userId
    ? `/api/v1/gamification/streak/me?userId=${encodeURIComponent(userId)}`
    : `/api/v1/gamification/streak/me`;

  const response = await proxyBackend(request, targetPath);
  if (response.status === 503) {
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
  return response;
}
