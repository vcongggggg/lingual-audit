import { NextRequest, NextResponse } from 'next/server';

const BACKEND_API_URL = process.env.BACKEND_API_URL || 'http://localhost:5224';

export async function GET(
  _req: NextRequest,
  context: { params: Promise<{ matchId: string }> }
) {
  const { matchId } = await context.params;
  try {
    const res = await fetch(`${BACKEND_API_URL}/api/v1/gamification/duels/${matchId}`, {
      cache: 'no-store',
      signal: AbortSignal.timeout(5000),
    });
    if (res.ok) {
      const json = await res.json();
      return NextResponse.json(json?.data ?? json);
    }
  } catch {
    // Fallback below
  }

  return NextResponse.json({
    id: matchId,
    challengerId: '00000000-0000-0000-0000-000000000001',
    challengerName: 'Chiến binh LINGUAL',
    opponentId: '00000000-0000-0000-0000-00000000bb07',
    opponentName: '🦉 LingLing AI Bot',
    status: 'accepted',
    questionCount: 5,
    challengerScore: 0,
    opponentScore: 0,
    challengerCorrect: 0,
    opponentCorrect: 0,
    winnerId: null,
    challengerXpAwarded: 0,
    opponentXpAwarded: 0,
    createdAt: new Date().toISOString(),
    startedAt: new Date().toISOString(),
    completedAt: null,
    currentRound: 1,
    isBotMatch: true,
  });
}

export async function POST(
  req: NextRequest,
  context: { params: Promise<{ matchId: string }> }
) {
  const { matchId } = await context.params;
  let body: Record<string, unknown> = {};
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: 'Invalid JSON payload' }, { status: 400 });
  }

  const action = (body.action as string) || 'submit';
  const endpoint =
    action === 'forfeit'
      ? `${BACKEND_API_URL}/api/v1/gamification/duels/${matchId}/forfeit`
      : action === 'accept'
      ? `${BACKEND_API_URL}/api/v1/gamification/duels/${matchId}/accept`
      : `${BACKEND_API_URL}/api/v1/gamification/duels/${matchId}/submit`;

  try {
    const res = await fetch(endpoint, {
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
    // Fallback below
  }

  return NextResponse.json({ accepted: true, status: action === 'forfeit' ? 'forfeit' : 'ok' });
}

