import { NextResponse } from "next/server";

export async function GET() {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const response = await fetch(`${apiBase}/api/v1/content/vocabulary/groups`, { cache: "no-store" });
    if (!response.ok) return NextResponse.json({ error: "Vocabulary groups unavailable" }, { status: response.status });
    return NextResponse.json(await response.json());
  } catch {
    return NextResponse.json({ error: "Could not connect to the vocabulary database" }, { status: 503 });
  }
}
