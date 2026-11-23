"use client";

import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import {
  ArrowRight,
  Award,
  CheckCircle2,
  Gamepad2,
  Keyboard,
  Target,
  X,
} from "lucide-react";
import { useLanguage } from "@/lib/language-context";

export type RealGameApiKey =
  | "word_match"
  | "sentence_scramble"
  | "typing_race"
  | "fill_blitz"
  | "lingo_wordle"
  | "sound_reflex";

interface GameTutorialSpec {
  titleVi: string;
  titleEn: string;
  goalVi: string;
  goalEn: string;
  controlsVi: string;
  controlsEn: string;
  scoringVi: string;
  scoringEn: string;
}

const GAME_TUTORIALS: Record<RealGameApiKey, GameTutorialSpec> = {
  word_match: {
    titleVi: "Nối từ siêu tốc",
    titleEn: "Speed Word Match",
    goalVi: "Ghép đúng toàn bộ các cặp từ tiếng Anh với nghĩa tiếng Việt tương ứng trong giới hạn 60 giây.",
    goalEn: "Match every English vocabulary card with its Vietnamese meaning within the 60-second timer.",
    controlsVi: "Bấm chọn 1 thẻ tiếng Anh rồi bấm thẻ nghĩa tiếng Việt (hoặc ngược lại). Nếu chọn nhầm, bấm lại thẻ đang chọn để hủy.",
    controlsEn: "Tap an English card and then tap its Vietnamese meaning (or vice versa). Tap a selected card again to deselect it.",
    scoringVi: "Mỗi cặp đúng +25 điểm nhân Combo (tối đa x3 = 75 điểm/cặp). Hoàn thành sạch bảng nhận thưởng +25 đến +35 XP và đồng bộ máy chủ.",
    scoringEn: "Each pair awards +25 pts multiplied by your Combo (up to x3 = 75 pts/pair). Clearing the board earns +25 to +35 XP verified on the server.",
  },
  sentence_scramble: {
    titleVi: "Xếp câu chuẩn",
    titleEn: "Sentence Scramble",
    goalVi: "Đọc câu gợi ý nghĩa tiếng Việt và ghép các mảnh từ bị xáo trộn thành câu tiếng Anh hoàn chỉnh đúng ngữ pháp trong 60 giây.",
    goalEn: "Read the Vietnamese meaning prompt and arrange the scrambled word chips into a grammatically correct English sentence within 60 seconds.",
    controlsVi: "Chạm từng mảnh từ bên dưới để đưa lên khung đáp án; chạm mảnh đã đặt để gỡ ra. Dùng nút Xếp lại, Kiểm tra câu hoặc Bỏ qua; sai 2 lần sẽ hiện gợi ý từ đầu/cuối.",
    controlsEn: "Tap word chips below to place them; tap placed chips to move them back. Use Reset, Check sentence, or Skip; after 2 misses a first/last-word hint appears.",
    scoringVi: "Mỗi câu xếp chính xác +30 điểm. Hoàn thành trọn bộ nhận +30 đến +35 XP được máy chủ xác thực.",
    scoringEn: "Each correct sentence gives +30 pts. Completing the round awards +30 to +35 XP verified by the server.",
  },
  typing_race: {
    titleVi: "Đua tốc độ gõ",
    titleEn: "Typing Speedster",
    goalVi: "Quan sát từ vựng tiếng Anh, phiên âm IPA và nghĩa tiếng Việt, sau đó gõ lại thật nhanh và chính xác trong 60 giây.",
    goalEn: "Read the English word, IPA transcription, and Vietnamese meaning, then type the word accurately as fast as you can within 60 seconds.",
    controlsVi: "Nhập trực tiếp trên bàn phím vào ô gõ. Hệ thống tự động nhận diện và chuyển sang từ tiếp theo ngay khi gõ khớp chính xác, không cần bấm Enter.",
    controlsEn: "Type directly into the input box. The game automatically advances to the next word the instant your spelling matches — no Enter key needed.",
    scoringVi: "Mỗi từ gõ đúng +40 điểm. Tích lũy tốc độ cao trong 60 giây để nhận thưởng +40 XP và lên bảng vàng Game Center.",
    scoringEn: "Each completed word scores +40 pts. Keep up your speed during the 60s sprint to earn +40 XP and climb the Game Center leaderboard.",
  },
  fill_blitz: {
    titleVi: "Điền khuyết ngữ cảnh",
    titleEn: "Context Cloze Blitz",
    goalVi: "Đọc câu ngữ cảnh tiếng Anh và chọn 1 trong 4 phương án A, B, C, D chính xác nhất trong thời gian 60 giây.",
    goalEn: "Read each English context prompt and pick the single best answer among choices A, B, C, and D within 60 seconds.",
    controlsVi: "Bấm vào phương án A/B/C/D để chốt đáp án, xem đáp án chuẩn ngay lập tức rồi bấm Câu tiếp theo để sang câu kế.",
    controlsEn: "Click option A/B/C/D to lock in your answer, check the immediate feedback, then click Next question to continue.",
    scoringVi: "Mỗi câu trả lời đúng +20 điểm. Hoàn thành bài nhận +20 đến +25 XP theo độ chính xác được chấm trên máy chủ.",
    scoringEn: "Each correct answer awards +20 pts. Finishing the set earns +20 to +25 XP based on server-verified accuracy.",
  },
  sound_reflex: {
    titleVi: "Phản xạ nghe âm",
    titleEn: "Audio Ear Reflex",
    goalVi: "Lắng nghe phát âm từ vựng tiếng Anh và chọn nhanh nghĩa tiếng Việt đúng nhất trong 4 phương án trong 60 giây.",
    goalEn: "Listen to the spoken English vocabulary word and choose the matching Vietnamese meaning among 4 options within 60 seconds.",
    controlsVi: "Âm thanh tự phát khi sang câu mới; bấm vào biểu tượng Loa bất cứ lúc nào để nghe lại, rồi chọn đáp án 1–4.",
    controlsEn: "Pronunciation plays automatically for each prompt; tap the Speaker icon anytime to replay, then pick option 1–4.",
    scoringVi: "Mỗi câu nghe chính xác +35 điểm và tự động chuyển tiếp. Hoàn thành vòng chơi nhận thưởng +35 XP.",
    scoringEn: "Each accurate listening pick gives +35 pts and auto-advances. Completing the round earns +35 XP.",
  },
  lingo_wordle: {
    titleVi: "Lingo Wordle",
    titleEn: "5-Letter Word Guess",
    goalVi: "Suy luận từ tiếng Anh bí mật gồm 5 chữ cái (có gợi ý chủ đề và nghĩa tiếng Việt) trong tối đa 6 lượt đoán, không giới hạn thời gian.",
    goalEn: "Deduce the secret 5-letter English word (using the topic & Vietnamese meaning hints) within 6 guesses with no time limit.",
    controlsVi: "Gõ trực tiếp trên bàn phím thật hoặc bấm bàn phím ảo (Enter để gửi, Backspace để xóa). Xanh lá = đúng vị trí · Vàng = có trong từ nhưng sai vị trí · Xám = không có.",
    controlsEn: "Type on your physical keyboard or tap the on-screen keys (Enter to submit, Backspace to delete). Green = right spot · Gold = wrong spot · Gray = not in word.",
    scoringVi: "Đoán trúng trong 6 lượt nhận 100 điểm & +50 XP (chấm hoàn toàn trên máy chủ); hết lượt vẫn nhận điểm nỗ lực.",
    scoringEn: "Solving within 6 tries awards 100 pts & +50 XP (evaluated server-side); unfinished rounds still grant effort credit.",
  },
};

export function getGameTutorialStorageKey(apiKey: string): string {
  return `lingual_game_tutorial_${apiKey}`;
}

export function isGameTutorialDismissed(apiKey: string): boolean {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(getGameTutorialStorageKey(apiKey)) === "true";
}

interface GameTutorialModalProps {
  gameApiKey: string;
  isOpen: boolean;
  onClose: () => void;
  onStart: () => void;
  actionLabel?: string;
}

export default function GameTutorialModal({
  gameApiKey,
  isOpen,
  onClose,
  onStart,
  actionLabel,
}: GameTutorialModalProps) {
  const { translate: t, language } = useLanguage();
  const [dontShowAgain, setDontShowAgain] = useState(true);

  useEffect(() => {
    if (!isOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isOpen, gameApiKey, onClose]);

  if (!isOpen || typeof document === "undefined") return null;

  const spec =
    GAME_TUTORIALS[gameApiKey as RealGameApiKey] ?? GAME_TUTORIALS.word_match;

  const handleConfirm = () => {
    if (dontShowAgain && typeof window !== "undefined") {
      window.localStorage.setItem(getGameTutorialStorageKey(gameApiKey), "true");
    }
    onStart();
  };

  return createPortal(
    <div
      className="tutorial-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="game-tutorial-title"
    >
      <div className="tutorial-modal game-tutorial-modal">
        <div className="tutorial-modal-top">
          <span className="tutorial-step-pill">
            <Gamepad2 size={13} aria-hidden="true" />
            {t("HƯỚNG DẪN LUẬT CHƠI", "HOW TO PLAY")}
          </span>
          <button
            type="button"
            className="tutorial-close-btn"
            onClick={onClose}
            aria-label={t("Đóng luật chơi", "Close instructions")}
          >
            <X size={17} />
          </button>
        </div>

        <div className="game-tutorial-header">
          <h2 id="game-tutorial-title">
            {language === "vi" ? spec.titleVi : spec.titleEn}
          </h2>
          <p>{language === "vi" ? spec.titleEn : spec.titleVi}</p>
        </div>

        <div className="game-tutorial-steps">
          <div className="game-tutorial-step">
            <div className="game-tutorial-step-icon">
              <Target size={18} aria-hidden="true" />
            </div>
            <div>
              <strong>1. {t("Mục tiêu", "Objective")}</strong>
              <p>{language === "vi" ? spec.goalVi : spec.goalEn}</p>
            </div>
          </div>

          <div className="game-tutorial-step">
            <div className="game-tutorial-step-icon">
              <Keyboard size={18} aria-hidden="true" />
            </div>
            <div>
              <strong>2. {t("Cách thao tác & phím tắt", "Controls & shortcuts")}</strong>
              <p>{language === "vi" ? spec.controlsVi : spec.controlsEn}</p>
            </div>
          </div>

          <div className="game-tutorial-step">
            <div className="game-tutorial-step-icon">
              <Award size={18} aria-hidden="true" />
            </div>
            <div>
              <strong>3. {t("Tính điểm & Thưởng XP", "Scoring & XP rewards")}</strong>
              <p>{language === "vi" ? spec.scoringVi : spec.scoringEn}</p>
            </div>
          </div>
        </div>

        <label className="game-tutorial-checkbox">
          <input
            type="checkbox"
            checked={dontShowAgain}
            onChange={(e) => setDontShowAgain(e.target.checked)}
          />
          <CheckCircle2 size={15} aria-hidden="true" />
          <span>{t("Không tự hiện lại cho trò chơi này", "Don't show automatically again for this game")}</span>
        </label>

        <div className="tutorial-footer">
          <button type="button" className="tutorial-skip-btn" onClick={onClose}>
            {t("Đóng", "Close")}
          </button>
          <button type="button" className="tutorial-primary-btn" onClick={handleConfirm}>
            {actionLabel ?? t("Bắt đầu chơi ngay", "Start playing now")}
            <ArrowRight size={15} />
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
}
