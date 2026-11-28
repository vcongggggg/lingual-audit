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
    let targetPath = "/api/v1/ai/guest/roleplay";

    if (authHeader && authHeader.trim().length > 0) {
      headers["authorization"] = authHeader;
      targetPath = "/api/v1/ai/roleplay";
    } else {
      headers["x-forwarded-for"] = resolveClientIpHeader(request);
    }

    if (idempotencyKey) {
      headers["x-idempotency-key"] = idempotencyKey;
    }

    const rawBody = (await request.json()) as Record<string, unknown>;
    // Never forward client-supplied userId/mezonUserId to backend
    const sanitizedPayload = {
      scenarioCode: rawBody.scenarioCode,
      userMessage: rawBody.userMessage,
      conversationId: rawBody.conversationId ?? null,
    };

    const response = await fetch(`${apiBase}${targetPath}`, {
      method: "POST",
      headers,
      body: JSON.stringify(sanitizedPayload),
      cache: "no-store",
      signal: AbortSignal.timeout(30000),
    });

    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const data = await response.json();
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(
      { error: "AI Roleplay server returned an unexpected response." },
      { status: response.status || 502 }
    );
  } catch {
    return NextResponse.json(
      {
        code: "AI_SERVICE_UNREACHABLE",
        message: "Kết nối đến máy chủ AI tạm thời gián đoạn. Vui lòng thử lại sau ít phút.",
      },
      { status: 503 }
    );
  }
}
