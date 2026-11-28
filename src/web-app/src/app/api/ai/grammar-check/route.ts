import { NextRequest, NextResponse } from "next/server";

function resolveClientIpHeader(request: NextRequest): string {
  const forwarded = request.headers.get("x-forwarded-for");
  if (forwarded) return forwarded;
  const realIp = request.headers.get("x-real-ip");
  if (realIp) return realIp;
  return "127.0.0.1";
}

export async function POST(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const idempotencyKey = request.headers.get("x-idempotency-key");
    const headers: Record<string, string> = { "content-type": "application/json" };
    let targetPath = "/api/v1/ai/guest/grammar-check";

    if (authHeader && authHeader.trim().length > 0) {
      headers["authorization"] = authHeader;
      targetPath = "/api/v1/ai/grammar-check";
    } else {
      headers["x-forwarded-for"] = resolveClientIpHeader(request);
    }

    if (idempotencyKey) {
      headers["x-idempotency-key"] = idempotencyKey;
    }

    const payload = await request.json();
    const response = await fetch(`${apiBase}${targetPath}`, {
      method: "POST",
      headers,
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: AbortSignal.timeout(30000),
    });

    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const data = await response.json();
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(
      { error: "AI Grammar server error", isCorrect: false },
      { status: response.status || 502 }
    );
  } catch {
    return NextResponse.json(
      {
        code: "AI_SERVICE_UNREACHABLE",
        message: "Không thể kết nối đến máy chủ AI để kiểm tra ngữ pháp lúc này.",
        error: "Không thể kết nối đến máy chủ AI để kiểm tra ngữ pháp lúc này.",
        isCorrect: false,
      },
      { status: 503 }
    );
  }
}
