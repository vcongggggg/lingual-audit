import { NextRequest, NextResponse } from 'next/server';

const BACKEND_API_URL = process.env.BACKEND_API_URL || 'http://localhost:5224';

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url);
  const userId = searchParams.get('userId') || '';
  const limit = searchParams.get('limit') || '10';

  try {
    const query = new URLSearchParams({ limit });
    if (userId) query.set('userId', userId);
    const res = await fetch(`${BACKEND_API_URL}/api/v1/gamification/duels/history?${query.toString()}`, {
      cache: 'no-store',
      signal: AbortSignal.timeout(5000),
    });
    if (res.ok) {
      const json = await res.json();
      return NextResponse.json(json?.data ?? json);
    }
  } catch {
    // Fallback to empty history list when backend is offline
  }

  return NextResponse.json([]);
}

export async function POST(req: NextRequest) {
  let body: Record<string, unknown> = {};
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: 'Invalid JSON payload' }, { status: 400 });
  }

  try {
    const res = await fetch(`${BACKEND_API_URL}/api/v1/gamification/duels/challenge`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(5000),
    });
    if (res.ok) {
      const json = await res.json();
      return NextResponse.json(json?.data ?? json);
    }
  } catch {
    // Fallback match creation when backend is offline
  }

  const now = new Date().toISOString();
  return NextResponse.json({
    id: crypto.randomUUID(),
    challengerId: body.challengerId || '00000000-0000-0000-0000-000000000001',
    challengerName: body.challengerName || 'Bạn',
    opponentId: body.opponentId || '00000000-0000-0000-0000-00000000bb07',
    opponentName: body.opponentName || '🦉 LingLing AI Bot',
    status: body.autoAccept ? 'accepted' : 'pending',
    questionCount: 5,
    challengerScore: 0,
    opponentScore: 0,
    challengerCorrect: 0,
    opponentCorrect: 0,
    winnerId: null,
    challengerXpAwarded: 0,
    opponentXpAwarded: 0,
    createdAt: now,
    startedAt: null,
    completedAt: null,
    currentRound: 1,
    isBotMatch: true,
  });
}

