"use client";

import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import {
  ArrowLeft,
  ArrowRight,
  BookOpen,
  Bot,
  Compass,
  Gamepad2,
  NotebookTabs,
  Sparkles,
  Trophy,
  X,
} from "lucide-react";
import { useLanguage } from "@/lib/language-context";

export const ONBOARDING_STORAGE_KEY = "lingual_onboarding_v1_done";
export const OPEN_ONBOARDING_EVENT = "lingual:open-onboarding";

export function triggerOnboardingTour() {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent(OPEN_ONBOARDING_EVENT));
  }
}

type GroupsCountResponse = {
  groups?: { word_count?: number }[];
};

export default function OnboardingTour() {
  const { translate: t, language } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const [step, setStep] = useState(0);
  const [vocabTotal, setVocabTotal] = useState<number | null>(null);

  useEffect(() => {
    const done = window.localStorage.getItem(ONBOARDING_STORAGE_KEY);
    if (!done) {
      const timer = window.setTimeout(() => {
        setStep(0);
        setIsOpen(true);
      }, 450);
      return () => window.clearTimeout(timer);
    }
  }, []);

  useEffect(() => {
    const onOpen = () => {
      setStep(0);
      setIsOpen(true);
    };
    window.addEventListener(OPEN_ONBOARDING_EVENT, onOpen);
    return () => window.removeEventListener(OPEN_ONBOARDING_EVENT, onOpen);
  }, []);

  useEffect(() => {
    if (!isOpen || vocabTotal !== null) return;
    const controller = new AbortController();
    void fetch("/api/vocabulary/groups", { cache: "no-store", signal: controller.signal })
      .then((res) => (res.ok ? (res.json() as Promise<GroupsCountResponse>) : null))
      .then((data) => {
        if (data?.groups && Array.isArray(data.groups)) {
          const sum = data.groups.reduce((acc, g) => acc + (Number(g.word_count) || 0), 0);
          if (sum > 0) setVocabTotal(sum);
        }
      })
      .catch(() => {});
    return () => controller.abort();
  }, [isOpen, vocabTotal]);

  useEffect(() => {
    if (!isOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        window.localStorage.setItem(ONBOARDING_STORAGE_KEY, "true");
        setIsOpen(false);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isOpen]);

  if (!isOpen) return null;

  const locale = language === "vi" ? "vi-VN" : "en-US";
  const vocabLabelVi =
    vocabTotal !== null
      ? `${vocabTotal.toLocaleString(locale)} từ vựng chuẩn CEFR (A1–C2)`
      : "kho từ vựng chuẩn CEFR (A1–C2)";
  const vocabLabelEn =
    vocabTotal !== null
      ? `${vocabTotal.toLocaleString(locale)} CEFR (A1–C2) words`
      : "the CEFR (A1–C2) vocabulary library";

  const steps = [
    {
      icon: Compass,
      badge: t("TỔNG QUAN", "OVERVIEW"),
      title: t("Chào mừng đến với LinguaMezon!", "Welcome to LinguaMezon!"),
      description: t(
        "Mỗi ngày chỉ cần 5–10 phút: xem Từ vựng hôm nay ngay trên Trang chủ, theo dõi Mục tiêu ngày (10 từ & 50 XP) và duy trì chuỗi Streak cùng Clan Mezon.",
        "Just 5–10 minutes a day: discover the Word of the Day right on the Home dashboard, track your Daily Goal (10 words & 50 XP), and grow your streak with your Mezon Clan."
      ),
      tip: t(
        "Mẹo: Bạn có thể bấm nút ? trên thanh điều hướng bất cứ lúc nào để mở lại hướng dẫn này.",
        "Tip: Click the ? button on the top navigation bar anytime to reopen this guide."
      ),
    },
    {
      icon: BookOpen,
      badge: t("HỌC TỪ · FLASHCARD SM-2", "VOCABULARY · SM-2 FLASHCARDS"),
      title: t("Kho từ vựng & Flashcard lặp lại ngắt quãng", "Vocabulary Library & Spaced Repetition"),
      description: t(
        `Khám phá ${vocabLabelVi} chia theo chủ đề tại mục Học từ. Chuyển nhanh sang Flashcard 2 cột để nghe phát âm, lật thẻ (Space) và tự đánh giá 4 mức độ nhớ (phím 1–4) theo thuật toán SM-2.`,
        `Explore ${vocabLabelEn} organized by topic under Vocabulary. Switch directly to the 2-column Flashcards cockpit to listen, flip (Space), and rate recall (keys 1–4) with the SM-2 algorithm.`
      ),
      tip: t(
        "Mẹo: Dùng thanh lọc cấp độ A1–C2 ngay trên trang Flashcard để học đúng trình độ.",
        "Tip: Use the A1–C2 level filter at the top of Flashcards to match your exact level."
      ),
    },
    {
      icon: NotebookTabs,
      badge: t("LUYỆN TẬP", "PRACTICE"),
      title: t("Quiz đa dạng kỹ năng & Biểu đồ tiến độ", "Multi-skill Quiz & Activity Chart"),
      description: t(
        "Làm các phiên Quiz 10–20 câu (trắc nghiệm, ghép từ, nghe, xếp câu) để nhận phản hồi tức thì, cộng điểm XP và tự động vẽ biểu đồ hoạt động 7 ngày / 30 ngày.",
        "Complete 10–20 question quizzes (multiple choice, matching, dictation, scramble) for instant feedback, XP rewards, and live 7-day / 30-day progress charts."
      ),
      tip: t(
        "Mẹo: Hoàn thành 1 bài Quiz đầu tiên để mở khóa huy hiệu Bắt đầu ngay hôm nay.",
        "Tip: Finish your first Quiz to unlock the First Step badge today."
      ),
    },
    {
      icon: Gamepad2,
      badge: t("ĐẤU TRƯỜNG · 6 MINI-GAME & 1VS1", "ARENA · 6 MINI-GAMES & 1V1"),
      title: t("Game Center phản xạ & Đấu trường 1vs1", "Reflex Game Center & 1v1 Arena"),
      description: t(
        "Rèn phản xạ qua 6 mini-game tương tác (Nối từ, Xếp câu, Đua gõ, Điền khuyết, Nghe âm, Wordle) có hiệu ứng âm thanh và luật chơi riêng, hoặc chuyển sang tab Đấu trường 1vs1 để so tài tốc độ.",
        "Sharpen reflexes with 6 interactive mini-games (Word Match, Scramble, Typing Race, Context Cloze, Audio Reflex, Wordle) featuring sound effects and quick tutorials, or switch to the 1v1 Arena tab."
      ),
      tip: t(
        "Mẹo: Giữ chuỗi trả lời đúng liên tiếp để kích hoạt điểm Combo x2–x3.",
        "Tip: Chain consecutive right answers to multiply your score with Combo x2–x3."
      ),
    },
    {
      icon: Bot,
      badge: t("TRỢ LÝ AI LINGLING", "LINGLING AI TUTOR"),
      title: t("Đóng vai hội thoại & Sửa ngữ pháp 24/7", "24/7 Roleplay & Grammar Coach"),
      description: t(
        "Thực hành giao tiếp tiếng Anh theo kịch bản thực tế (Quán cà phê, Sân bay, Phỏng vấn) hoặc dán bất kỳ câu tiếng Anh nào để LingLing AI phân tích lỗi sai và gợi ý cách diễn đạt tự nhiên.",
        "Practice real-life English conversations (Cafe, Airport, Job Interview) or paste any English sentence for instant grammar corrections and natural phrasing tips from LingLing AI."
      ),
      tip: t(
        "Mẹo: Chọn các câu gợi ý nhanh dưới khung chat để mở lời tự nhiên hơn.",
        "Tip: Tap the quick prompt chips below the chat box to jump right into a scenario."
      ),
    },
    {
      icon: Trophy,
      badge: t("GIẢI ĐẤU & HỒ SƠ", "LEADERBOARD & PROFILE"),
      title: t("Đua top Clan & Sưu tầm 6 Huy hiệu danh dự", "Climb the Clan Leaderboard & Collect Badges"),
      description: t(
        "Đăng nhập Mezon SSO để đồng bộ mọi XP giữa Web App và Bot @LingLing trên kênh Clan, theo dõi thứ hạng tuần tại Giải đấu và mở khóa 6 Huy hiệu danh dự trong Hồ sơ cá nhân.",
        "Sign in with Mezon SSO to sync all XP between the Web App and @LingLing Bot in your Clan channel, track weekly ranks on the Leaderboard, and unlock 6 Honor Badges in your Profile."
      ),
      tip: t(
        "Sẵn sàng rồi! Chúc bạn có những phiên học hiệu quả cùng LinguaMezon.",
        "You are all set! Enjoy productive daily learning sessions with LinguaMezon."
      ),
    },
  ];

  const current = steps[step];
  const IconComp = current.icon;
  const isLast = step === steps.length - 1;

  if (!isOpen || typeof document === "undefined") return null;

  const finishTour = () => {
    window.localStorage.setItem(ONBOARDING_STORAGE_KEY, "true");
    setIsOpen(false);
  };

  return createPortal(
    <div
      className="tutorial-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="onboarding-tour-title"
    >
      <div className="tutorial-modal">
        <div className="tutorial-modal-top">
          <span className="tutorial-step-pill">
            <Sparkles size={13} aria-hidden="true" />
            {t(`Bước ${step + 1} / ${steps.length}`, `Step ${step + 1} / ${steps.length}`)}
          </span>
          <button
            type="button"
            className="tutorial-close-btn"
            onClick={finishTour}
            aria-label={t("Đóng hướng dẫn", "Close guide")}
          >
            <X size={17} />
          </button>
        </div>

        <div className="tutorial-progress-bar" aria-hidden="true">
          <span style={{ width: `${((step + 1) / steps.length) * 100}%` }} />
        </div>

        <div className="tutorial-body">
          <div className="tutorial-icon-badge">
            <IconComp size={26} aria-hidden="true" />
          </div>
          <p className="tutorial-eyebrow">{current.badge}</p>
          <h2 id="onboarding-tour-title">{current.title}</h2>
          <p className="tutorial-desc">{current.description}</p>
          <div className="tutorial-tip-box">{current.tip}</div>
        </div>

        <div className="tutorial-dots" aria-hidden="true">
          {steps.map((_, idx) => (
            <button
              key={idx}
              type="button"
              className={`tutorial-dot ${idx === step ? "is-active" : ""}`}
              onClick={() => setStep(idx)}
              aria-label={t(`Đi tới bước ${idx + 1}`, `Go to step ${idx + 1}`)}
            />
          ))}
        </div>

        <div className="tutorial-footer">
          <button type="button" className="tutorial-skip-btn" onClick={finishTour}>
            {t("Bỏ qua", "Skip")}
          </button>
          <div className="tutorial-nav-btns">
            {step > 0 && (
              <button
                type="button"
                className="tutorial-secondary-btn"
                onClick={() => setStep((s) => Math.max(0, s - 1))}
              >
                <ArrowLeft size={15} /> {t("Quay lại", "Back")}
              </button>
            )}
            <button
              type="button"
              className="tutorial-primary-btn"
              onClick={() => {
                if (isLast) finishTour();
                else setStep((s) => Math.min(steps.length - 1, s + 1));
              }}
            >
              {isLast ? t("Bắt đầu học ngay", "Start learning") : t("Tiếp theo", "Next")}
              <ArrowRight size={15} />
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
}
