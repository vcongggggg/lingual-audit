import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = { "content-type": "application/json" };
    if (authHeader) {
      headers["authorization"] = authHeader;
    }

    const payload = await request.json();
    const response = await fetch(`${apiBase}/api/v1/content/quiz-attempts`, {
      method: "POST",
      headers,
      body: JSON.stringify(payload),
      cache: "no-store",
    });

    const data = await response.json();
    return NextResponse.json(data, { status: response.status });
  } catch {
    return NextResponse.json({ error: "Could not save quiz attempt" }, { status: 503 });
  }
}
