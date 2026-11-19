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
        { error: "AI Grammar server error", isCorrect: false },
        { status: response.status }
      );
    }
    const data = await response.json();
    return NextResponse.json(data);
  } catch {
    return NextResponse.json(
      {
        error: "Không thể kết nối đến máy chủ AI để kiểm tra ngữ pháp lúc này.",
        isCorrect: false,
        explanation: "Hệ thống AI đang tạm thời gián đoạn kết nối. Vui lòng thử lại sau.",
      },
      { status: 503 }
    );
  }
}
