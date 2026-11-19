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
  const { language } = useLanguage();
  const message = moodCopy[mood][language];
  return (
    <div className={`lingua-mascot lingua-mascot-${mood} ${compact ? "is-compact" : ""}`} role="img" aria-label={`Mascot: ${message}`}>
      <span className="mascot-aura" aria-hidden="true"><Sparkles size={14} /></span>
      <svg className="mascot-figure" viewBox="0 0 120 120" aria-hidden="true">
        <path className="mascot-body" d="M25 67c0-25 15-43 35-43s35 18 35 43v18c0 12-10 20-35 20S25 97 25 85Z" />
        <path className="mascot-face" d="M31 57c0-22 12-35 29-35s29 13 29 35v9c0 18-13 30-29 30S31 84 31 66Z" />
        <circle className="mascot-eye" cx="49" cy="63" r="4" />
        <circle className="mascot-eye" cx="71" cy="63" r="4" />
        <path className="mascot-mouth" d="M51 76c5 7 13 7 18 0" />
        <path className="mascot-arm" d="M27 74 13 83M93 74l14 9" />
        <path className="mascot-leaf" d="M60 20c-5-13 5-18 15-18-1 10-6 16-15 18Z" />
      </svg>
      {!compact && <p>{message}</p>}
    </div>
  );
}
