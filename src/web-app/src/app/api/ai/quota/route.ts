import { NextRequest, NextResponse } from "next/server";

function resolveClientIpHeader(request: NextRequest): string {
  const forwarded = request.headers.get("x-forwarded-for");
  if (forwarded) return forwarded;
  const realIp = request.headers.get("x-real-ip");
  if (realIp) return realIp;
  return "127.0.0.1";
}

export async function GET(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = {};
    let targetPath = "/api/v1/ai/guest/quota";

    if (authHeader && authHeader.trim().length > 0) {
      headers["authorization"] = authHeader;
      targetPath = "/api/v1/ai/quota";
    } else {
      headers["x-forwarded-for"] = resolveClientIpHeader(request);
    }

    const response = await fetch(`${apiBase}${targetPath}`, {
      method: "GET",
      headers,
      cache: "no-store",
      signal: AbortSignal.timeout(10000),
    });

    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const data = await response.json();
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(
      { error: "AI quota service returned a non-JSON response" },
      { status: response.status || 502 }
    );
  } catch {
    return NextResponse.json(
      { error: "Không thể kết nối máy chủ kiểm tra lượt AI." },
      { status: 503 }
    );
  }
}
