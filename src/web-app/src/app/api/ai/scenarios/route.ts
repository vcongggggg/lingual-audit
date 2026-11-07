import { NextResponse } from "next/server";

export async function GET() {
  const apiBase = process.env.LINGUAL_API_URL ?? "http://localhost:5000";
  try {
    const response = await fetch(`${apiBase}/api/v1/ai/scenarios`, {
      cache: "no-store",
      signal: AbortSignal.timeout(10000),
    });
    if (!response.ok) {
      return NextResponse.json({ error: "AI Scenarios unavailable" }, { status: response.status });
    }
    const data = await response.json();
    return NextResponse.json(data);
  } catch {
    // Fallback static scenarios when backend offline
    const fallbackScenarios = [
      {
        id: "11111111-1111-1111-1111-111111111101",
        code: "cafe_order",
        title: "Gọi đồ uống tại Quán Cà phê (London)",
        description: "Luyện giao tiếp gọi cà phê/trà, chọn size, loại sữa và thanh toán với nhân viên pha chế tại London.",
        level: "A1",
        characterRole: "Barista LingLing",
        greetingMessage: "Hi there! Welcome to The London Roast. What drink can I get started for you today?",
      },
      {
        id: "22222222-2222-2222-2222-222222222202",
        code: "airport_checkin",
        title: "Làm thủ tục tại Sân bay Quốc tế",
        description: "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành tại sân bay Heathrow.",
        level: "A2",
        characterRole: "Ground Crew LingLing",
        greetingMessage: "Good morning! Welcome to London Heathrow Airport. May I see your passport and flight booking, please?",
      },
      {
        id: "33333333-3333-3333-3333-333333333303",
        code: "job_interview",
        title: "Phỏng vấn xin việc & Giới thiệu bản thân",
        description: "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh và định hướng nghề nghiệp.",
        level: "B1",
        characterRole: "Interviewer LingLing",
        greetingMessage: "Hello! Welcome to our interview today. To start off, could you tell me a little bit about yourself and your background?",
      },
    ];
    return NextResponse.json(fallbackScenarios);
  }
}

