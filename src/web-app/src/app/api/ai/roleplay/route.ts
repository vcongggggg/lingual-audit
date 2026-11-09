import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const payload = await request.json();
    const response = await fetch(`${apiBase}/api/v1/ai/roleplay`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: AbortSignal.timeout(15000),
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "AI Roleplay server returned an error" },
        { status: response.status }
      );
    }
    const data = await response.json();
    return NextResponse.json(data);
  } catch {
    // Graceful offline fallback response
    return NextResponse.json({
      conversationId: "fallback-conv-id",
      scenarioCode: "roleplay",
      scenarioTitle: "Roleplay Tình huống",
      characterRole: "Mascot LingLing",
      reply: "I heard your sentence! Keep practicing with me to improve your English fluency.",
      hasFeedback: false,
      feedbackCorrection: null,
      explanation: null,
    });
  }
}

