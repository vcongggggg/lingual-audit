import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = {};
    if (authHeader) headers.authorization = authHeader;
    const response = await fetch(`${apiBase}/api/v1/content/reviews`, {
      headers,
      cache: "no-store",
    });
    const body = await response.text();
    let data: unknown = {};
    try { data = body ? JSON.parse(body) : { error: "Sign in to view review progress." }; } catch { data = { error: body }; }
    return NextResponse.json(data, { status: response.status });
  } catch {
    return NextResponse.json({ error: "Could not load review progress" }, { status: 503 });
  }
}

export async function POST(request: NextRequest) {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const authHeader = request.headers.get("authorization");
    const headers: Record<string, string> = { "content-type": "application/json" };
    if (authHeader) {
      headers["authorization"] = authHeader;
    }

    const payload = await request.json();
    const response = await fetch(`${apiBase}/api/v1/content/reviews`, {
      method: "POST",
      headers,
      body: JSON.stringify(payload),
      cache: "no-store",
    });

    const body = await response.text();
    let data: unknown = {};
    try { data = body ? JSON.parse(body) : { error: "Could not save review" }; } catch { data = { error: body }; }
    return NextResponse.json(data, { status: response.status });
  } catch {
    return NextResponse.json({ error: "Could not save review" }, { status: 503 });
  }
}
