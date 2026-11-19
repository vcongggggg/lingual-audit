import { NextRequest } from "next/server";
import { proxyBackend } from "@/app/api/api";

export async function POST(request: NextRequest) {
  return proxyBackend(request, "/api/v1/content/reviews");
}
