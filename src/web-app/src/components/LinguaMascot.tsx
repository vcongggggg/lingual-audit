"use client";

import { Sparkles } from "lucide-react";

export type MascotMood = "idle" | "thinking" | "correct" | "encourage" | "complete";

const moodCopy: Record<MascotMood, string> = {
  idle: "Mình ở đây để cùng bạn học.",
  thinking: "Cứ bình tĩnh, bạn đang suy nghĩ rất tốt.",
  correct: "Tuyệt vời! Bạn bắt đúng nhịp rồi.",
  encourage: "Không sao, mình cùng thử lại nhé.",
  complete: "Một chặng nhỏ đã hoàn thành!",
};

export default function LinguaMascot({ mood = "idle", compact = false }: { mood?: MascotMood; compact?: boolean }) {
  // Mascot cow completely removed per project guidelines
  return null;
}
