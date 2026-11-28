import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = {};
    let targetPath = "/api/v1/ai/guest/scenarios";

    if (authHeader && authHeader.trim().length > 0) {
      headers["authorization"] = authHeader;
      targetPath = "/api/v1/ai/scenarios";
    }

    const response = await fetch(`${apiBase}${targetPath}`, {
      headers,
      cache: "no-store",
      signal: AbortSignal.timeout(10000),
    });
    if (!response.ok) {
      return NextResponse.json({ error: "AI Scenarios unavailable" }, { status: response.status });
    }
    const data = await response.json();
    return NextResponse.json(data);
  } catch {
    return NextResponse.json({ error: "Could not reach AI scenarios service" }, { status: 503 });
  }
}
