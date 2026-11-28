import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  const days = request.nextUrl.searchParams.get("days");
  return proxyBackend(request, "/api/v1/gamification/activity/weekly", {
    params: days ? { days } : undefined,
  });
}
