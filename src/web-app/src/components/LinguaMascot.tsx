"use client";

import { Sparkles } from "lucide-react";
import { useLanguage } from "@/lib/language-context";

export type MascotMood = "idle" | "thinking" | "correct" | "encourage" | "complete";

const moodCopy: Record<MascotMood, { vi: string; en: string }> = {
  idle: { vi: "Mình ở đây để cùng bạn học.", en: "I am here to learn with you." },
  thinking: { vi: "Cứ bình tĩnh, bạn đang suy nghĩ rất tốt.", en: "Take your time, you are thinking well." },
  correct: { vi: "Tuyệt vời! Bạn bắt đúng nhịp rồi.", en: "Great! You are on the right track." },
  encourage: { vi: "Không sao, mình cùng thử lại nhé.", en: "That is okay, let us try again together." },
  complete: { vi: "Một chặng nhỏ đã hoàn thành!", en: "One small milestone complete!" },
};

export default function LinguaMascot({ mood = "idle", compact = false }: { mood?: MascotMood; compact?: boolean }) {
  // Mascot cow completely removed per project guidelines
  return null;
}
