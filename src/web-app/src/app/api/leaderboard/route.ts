import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const scope = searchParams.get("scope") || "personal";
  const type = searchParams.get("type") || "all-time";
  const limit = searchParams.get("limit") || "20";

  return proxyBackend(
    request,
    `/api/v1/content/leaderboard?scope=${encodeURIComponent(scope)}&type=${encodeURIComponent(type)}&limit=${encodeURIComponent(limit)}`
  );
}
