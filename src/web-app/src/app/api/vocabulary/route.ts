import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  const limit = Math.min(500, Math.max(1, Number(request.nextUrl.searchParams.get("limit") ?? 50)));
  return proxyBackend(request, `/api/v1/content/vocabulary?limit=${limit}`);
}
