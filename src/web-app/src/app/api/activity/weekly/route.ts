import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function GET(request: NextRequest) {
  return proxyBackend(request, "/api/v1/gamification/activity/weekly");
}
