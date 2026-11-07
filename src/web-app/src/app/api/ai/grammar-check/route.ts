import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const payload = await request.json();
    const response = await fetch(`${apiBase}/api/v1/ai/grammar-check`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: AbortSignal.timeout(15000),
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "AI Grammar server error" },
        { status: response.status }
      );
    }
    const data = await response.json();
    return NextResponse.json(data);
  } catch {
    return NextResponse.json({
      isCorrect: true,
      correctedText: "",
      explanation: "Chưa kết nối được máy chủ AI, hãy thử lại sau ít phút.",
      tips: "Tiếp tục luyện tập thường xuyên!",
    });
  }
}

