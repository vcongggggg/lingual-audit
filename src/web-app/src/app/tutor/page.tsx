"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import LinguaMascot, { type MascotMood } from "@/components/LinguaMascot";
import {
  Send,
  Sparkles,
  Bot,
  RotateCcw,
  CheckCircle2,
  AlertCircle,
  Lightbulb,
  Coffee,
  Plane,
  Briefcase,
  HelpCircle,
  MessageSquare,
  ArrowRight,
  Zap,
  Gamepad2,
  Brain,
  BookOpen,
  LogIn,
  Clock,
} from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import { useAuth } from "@/components/auth/useAuth";

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface Scenario {
  id: string;
  code: string;
  title: string;
  description: string;
  level: string;
  characterRole: string;
  greetingMessage: string;
}

interface AiQuotaStatus {
  dailyFreeLimit: number;
  dailyUsed: number;
  dailyRemaining: number;
  bonusCredits: number;
  totalAvailable: number;
  dailyBonusEarned: number;
  dailyBonusCap: number;
  nextResetAtUtc: string;
  isGuest?: boolean;
}

interface ChatMessage {
  id: string;
  sender: "user" | "bot";
  text: string;
  timestamp: string;
  hasFeedback?: boolean;
  feedbackCorrection?: string | null;
  explanation?: string | null;
  tips?: string | null;
}

const DEFAULT_SCENARIOS: Scenario[] = [
  {
    id: "1",
    code: "cafe_order",
    title: "Quán Cà phê (London)",
    description: "Luyện gọi cà phê/trà, chọn size, loại sữa và thanh toán với Barista tại London.",
    level: "A1",
    characterRole: "Barista LingLing",
    greetingMessage: "Hi there! Welcome to The London Roast. What drink can I get started for you today?",
  },
  {
    id: "2",
    code: "airport_checkin",
    title: "Thủ tục Sân bay Quốc tế",
    description: "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành tại sân bay Heathrow.",
    level: "A2",
    characterRole: "Ground Crew LingLing",
    greetingMessage: "Good morning! Welcome to London Heathrow Airport. May I see your passport and flight booking, please?",
  },
  {
    id: "3",
    code: "job_interview",
    title: "Phỏng vấn xin việc",
    description: "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh và định hướng nghề nghiệp.",
    level: "B1",
    characterRole: "Interviewer LingLing",
    greetingMessage: "Hello! Welcome to our interview today. To start off, could you tell me a little bit about yourself and your background?",
  },
];

const SCENARIO_ICONS: Record<string, React.ElementType> = {
  cafe_order: Coffee,
  airport_checkin: Plane,
  job_interview: Briefcase,
};

const QUICK_PROMPTS: Record<string, string[]> = {
  cafe_order: [
    "I would like an iced latte with oat milk, please.",
    "Could I have a medium cappuccino and a chocolate croissant?",
    "How much does that cost in total?",
  ],
  airport_checkin: [
    "Here is my passport and flight booking confirmation.",
    "I have one suitcase to check in and one carry-on bag.",
    "Could you tell me which gate my flight departs from?",
  ],
  job_interview: [
    "I am a passionate software engineering student with experience in web and AI development.",
    "My greatest strength is problem-solving and rapid learning.",
    "I am looking for an opportunity where I can contribute to impactful real-time applications.",
  ],
};

function formatTimeLabel(): string {
  return new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
}

function buildInitialMessages(
  tab: "roleplay" | "grammar",
  scenario: Scenario,
  translate: (vi: string, en: string) => string
): ChatMessage[] {
  if (tab === "roleplay") {
    return [
      {
        id: "greeting-" + scenario.code,
        sender: "bot",
        text: scenario.greetingMessage,
        timestamp: "",
      },
    ];
  }
  return [
    {
      id: "grammar-intro",
      sender: "bot",
      text: translate(
        "Xin chào! Mình là Mascot LingLing. Bạn hãy nhập bất kỳ câu tiếng Anh nào vào khung bên dưới, mình sẽ phân tích ngữ pháp, chỉ ra lỗi sai và hướng dẫn cách diễn đạt chuẩn bản xứ nhé!",
        "Hello! I am LingLing Mascot. Enter any English sentence below and I will analyze the grammar, point out mistakes, and guide you toward natural phrasing."
      ),
      timestamp: "",
    },
  ];
}

export default function TutorPage() {
  const { translate: t } = useLanguage();
  const { user, token, isAuthenticated } = useAuth();
  const [scenarios, setScenarios] = useState<Scenario[]>(DEFAULT_SCENARIOS);
  const [selectedScenario, setSelectedScenario] = useState<Scenario>(DEFAULT_SCENARIOS[0]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"roleplay" | "grammar">("roleplay");
  const [messages, setMessages] = useState<ChatMessage[]>(() =>
    buildInitialMessages("roleplay", DEFAULT_SCENARIOS[0], (vi) => vi)
  );
  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [mascotMood, setMascotMood] = useState<MascotMood>("idle");
  const [quota, setQuota] = useState<AiQuotaStatus | null>(null);
  const [quotaErrorBanner, setQuotaErrorBanner] = useState<string | null>(null);
  const chatStreamRef = useRef<HTMLDivElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const msgSeqRef = useRef(0);

  const nextMessageId = (prefix: string) => {
    msgSeqRef.current += 1;
    return `${prefix}-${msgSeqRef.current}`;
  };

  const getAuthToken = useCallback((): string | null => {
    if (token) return token;
    if (typeof window !== "undefined") {
      return localStorage.getItem("accessToken");
    }
    return null;
  }, [token]);

  const fetchQuota = useCallback(async () => {
    try {
      const headers: Record<string, string> = {};
      const authToken = getAuthToken();
      if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

      const res = await fetch("/api/ai/quota", { headers, cache: "no-store" });
      if (res.ok) {
        const data = (await res.json()) as AiQuotaStatus;
        if (typeof data.totalAvailable === "number") {
          setQuota(data);
          if (data.totalAvailable > 0) {
            setQuotaErrorBanner(null);
          }
        }
      }
    } catch {
      // Keep UI responsive if quota endpoint is temporarily unreachable
    }
  }, [getAuthToken]);

  // Load scenarios on mount & when auth changes
  useEffect(() => {
    let active = true;
    async function loadScenarios() {
      try {
        const headers: Record<string, string> = {};
        const authToken = getAuthToken();
        if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

        const res = await fetch("/api/ai/scenarios", { headers, cache: "no-store" });
        if (res.ok && active) {
          const data = (await res.json()) as Scenario[];
          if (Array.isArray(data) && data.length > 0) {
            setScenarios(data);
            setSelectedScenario((prev) => data.find((item) => item.code === prev.code) ?? data[0]);
          }
        }
      } catch {
        // Default scenario list already initialized
      }
    }
    void loadScenarios();
    return () => {
      active = false;
    };
  }, [getAuthToken]);

  // Load AI Quota on mount and whenever login state changes
  useEffect(() => {
    let active = true;
    async function loadInitialQuota() {
      try {
        const headers: Record<string, string> = {};
        const authToken = getAuthToken();
        if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

        const res = await fetch("/api/ai/quota", { headers, cache: "no-store" });
        if (res.ok && active) {
          const data = (await res.json()) as AiQuotaStatus;
          if (typeof data.totalAvailable === "number") {
            setQuota(data);
            if (data.totalAvailable > 0) {
              setQuotaErrorBanner(null);
            }
          }
        }
      } catch {
        // Keep UI responsive if quota endpoint is temporarily unreachable
      }
    }
    void loadInitialQuota();
    return () => {
      active = false;
    };
  }, [getAuthToken, isAuthenticated, user?.id]);

  // Auto scroll inside the chat stream only after user interaction
  useEffect(() => {
    if (messages.length > 1 || loading) {
      const container = chatStreamRef.current;
      if (container) {
        container.scrollTo({ top: container.scrollHeight, behavior: "smooth" });
      }
    }
  }, [messages, loading]);

  const handleSelectTab = (tab: "roleplay" | "grammar") => {
    setActiveTab(tab);
    setConversationId(null);
    setMascotMood("idle");
    setMessages(buildInitialMessages(tab, selectedScenario, t));
  };

  const handleSelectScenario = (scenario: Scenario) => {
    setSelectedScenario(scenario);
    setConversationId(null);
    setMascotMood("idle");
    if (activeTab === "roleplay") {
      setMessages(buildInitialMessages("roleplay", scenario, t));
    }
  };

  const isGuestMode = quota?.isGuest ?? (!isAuthenticated && !getAuthToken());
  const isQuotaExhausted = quota !== null && quota.totalAvailable <= 0;

  const handleSendMessage = async (textToSend?: string) => {
    const text = (textToSend ?? inputMessage).trim();
    if (!text || loading) return;

    // Pre-flight guard: do not call Gemini when quota is already known to be 0
    if (quota !== null && quota.totalAvailable <= 0) {
      setQuotaErrorBanner(
        isGuestMode
          ? t(
              "Bạn đã dùng hết 2 lượt trải nghiệm miễn phí hôm nay. Đăng nhập bằng Mezon để nhận 5 lượt miễn phí/ngày và kiếm thêm lượt thưởng!",
              "You have used all 2 free guest turns today. Sign in with Mezon to get 5 free turns/day and earn bonus turns!"
            )
          : t(
              "Bạn đã dùng hết lượt hỏi LingLing AI hôm nay. Hãy chơi Game Center, làm Quiz hoặc ôn 5 thẻ Flashcard để kiếm thêm lượt thưởng!",
              "You have used all your LingLing AI turns today. Play Game Center, take a Quiz, or review 5 Flashcards to earn bonus turns!"
            )
      );
      return;
    }

    setInputMessage("");
    setLoading(true);
    setMascotMood("thinking");
    setQuotaErrorBanner(null);

    const userMsg: ChatMessage = {
      id: nextMessageId("user"),
      sender: "user",
      text,
      timestamp: formatTimeLabel(),
    };

    setMessages((prev) => [...prev, userMsg]);

    try {
      const headers: Record<string, string> = { "Content-Type": "application/json" };
      const authToken = getAuthToken();
      if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

      const endpoint = activeTab === "roleplay" ? "/api/ai/roleplay" : "/api/ai/grammar-check";
      const validConversationId = conversationId && UUID_REGEX.test(conversationId) ? conversationId : null;
      const bodyPayload =
        activeTab === "roleplay"
          ? {
              scenarioCode: selectedScenario.code,
              userMessage: text,
              conversationId: validConversationId,
            }
          : { text };

      const response = await fetch(endpoint, {
        method: "POST",
        headers,
        body: JSON.stringify(bodyPayload),
      });

      const responseData = (await response.json().catch(() => ({}))) as Record<string, unknown>;

      if (response.status === 429 || responseData.code === "AI_QUOTA_EXCEEDED") {
        const returnedQuota = responseData.quota as AiQuotaStatus | undefined;
        if (returnedQuota && typeof returnedQuota.totalAvailable === "number") {
          setQuota(returnedQuota);
        } else {
          setQuota((prev) =>
            prev
              ? { ...prev, dailyRemaining: 0, bonusCredits: 0, totalAvailable: 0 }
              : {
                  dailyFreeLimit: isGuestMode ? 2 : 5,
                  dailyUsed: isGuestMode ? 2 : 5,
                  dailyRemaining: 0,
                  bonusCredits: 0,
                  totalAvailable: 0,
                  dailyBonusEarned: 0,
                  dailyBonusCap: isGuestMode ? 0 : 5,
                  nextResetAtUtc: "",
                  isGuest: isGuestMode,
                }
          );
        }

        const msgText =
          typeof responseData.message === "string" && responseData.message.trim().length > 0
            ? responseData.message
            : t(
                "Bạn đã dùng hết lượt hỏi LingLing AI hôm nay.",
                "You have reached your LingLing AI limit for today."
              );

        setQuotaErrorBanner(msgText);
        setMessages((prev) => [
          ...prev,
          {
            id: nextMessageId("quota"),
            sender: "bot",
            text: `⏳ ${msgText}`,
            timestamp: formatTimeLabel(),
          },
        ]);
        setMascotMood("encourage");
        return;
      }

      if (!response.ok) {
        if (responseData.quota && typeof (responseData.quota as AiQuotaStatus).totalAvailable === "number") {
          setQuota(responseData.quota as AiQuotaStatus);
        }
        const serverMsg =
          typeof responseData.message === "string" && responseData.message.trim().length > 0
            ? responseData.message
            : t(
                "Kết nối đến máy chủ AI tạm thời gián đoạn. Lượt của bạn đã được bảo toàn, hãy thử lại sau ít phút.",
                "Connection to the AI server was temporarily interrupted. Your turn was preserved, please try again shortly."
              );
        throw new Error(serverMsg);
      }

      // Sync latest quota from backend response
      if (responseData.quota && typeof (responseData.quota as AiQuotaStatus).totalAvailable === "number") {
        setQuota(responseData.quota as AiQuotaStatus);
      } else {
        void fetchQuota();
      }

      if (activeTab === "roleplay") {
        const data = responseData as {
          conversationId?: string;
          reply?: string;
          hasFeedback?: boolean;
          feedbackCorrection?: string | null;
          explanation?: string | null;
        };

        if (data.conversationId && UUID_REGEX.test(data.conversationId)) {
          setConversationId(data.conversationId);
        }

        const botMsg: ChatMessage = {
          id: nextMessageId("bot"),
          sender: "bot",
          text: data.reply ?? "",
          timestamp: formatTimeLabel(),
          hasFeedback: Boolean(data.hasFeedback),
          feedbackCorrection: data.feedbackCorrection ?? null,
          explanation: data.explanation ?? null,
        };

        setMessages((prev) => [...prev, botMsg]);
        setMascotMood(data.hasFeedback ? "encourage" : "correct");
      } else {
        const data = responseData as {
          isCorrect?: boolean;
          correctedText?: string;
          explanation?: string;
          tips?: string;
          error?: string;
        };

        if (data.error || typeof data.isCorrect !== "boolean") {
          throw new Error(data.error || "Grammar evaluation unavailable");
        }

        const botReply = data.isCorrect
          ? t(
              "🎉 Tuyệt vời! Câu của bạn hoàn toàn chính xác về ngữ pháp và cách dùng từ tự nhiên.",
              "🎉 Great job! Your sentence is grammatically accurate and natural."
            )
          : t(
              "💡 LingLing đã kiểm tra câu của bạn. Có một số điểm cần lưu ý để câu tự nhiên hơn!",
              "💡 LingLing reviewed your sentence. Here are a few suggestions to make it sound more natural!"
            );

        const botMsg: ChatMessage = {
          id: nextMessageId("bot"),
          sender: "bot",
          text: botReply,
          timestamp: formatTimeLabel(),
          hasFeedback: !data.isCorrect,
          feedbackCorrection: data.correctedText ? `${t("Nên nói", "Better phrasing")}: "${data.correctedText}"` : null,
          explanation: data.explanation ?? null,
          tips: data.tips ?? null,
        };

        setMessages((prev) => [...prev, botMsg]);
        setMascotMood(data.isCorrect ? "correct" : "encourage");
      }
    } catch (err: unknown) {
      const errorMsgText =
        err instanceof Error && err.message
          ? `⚠️ ${err.message}`
          : activeTab === "grammar"
            ? t(
                "⚠️ Không thể kết nối đến máy chủ AI để kiểm tra ngữ pháp lúc này. Vui lòng thử lại sau ít phút.",
                "⚠️ Could not connect to the AI grammar server right now. Please try again in a moment."
              )
            : t(
                "⚠️ Kết nối đến máy chủ AI tạm thời gián đoạn. Vui lòng thử lại sau ít phút.",
                "⚠️ Connection to the AI server was temporarily interrupted. Please try again shortly."
              );

      setMessages((prev) => [
        ...prev,
        {
          id: nextMessageId("err"),
          sender: "bot",
          text: errorMsgText,
          timestamp: formatTimeLabel(),
        },
      ]);
      setMascotMood("encourage");
    } finally {
      setLoading(false);
    }
  };

  const resetSession = () => {
    setConversationId(null);
    setMessages(buildInitialMessages("roleplay", selectedScenario, t));
    setMascotMood("idle");
  };

  return (
    <div className="tutor-container">
      {/* Header Banner */}
      <header className="tutor-header">
        <div className="tutor-header-main">
          <div className="tutor-title-box">
            <span className="tutor-badge">
              <Sparkles size={14} /> AI TUTOR • GOOGLE GEMINI
            </span>
            <h1 className="tutor-title">
              {t("Luyện Giao Tiếp cùng Mascot LingLing", "Practice conversation with LingLing")}
            </h1>
            <p className="tutor-desc">
              {t(
                "Thực hành đàm thoại tiếng Anh theo ngữ cảnh thực tế, nhận phản hồi ngữ pháp tức thì và nâng cao phản xạ tự nhiên.",
                "Practice English conversation in real-world contexts, get instant grammar feedback, and build natural responses."
              )}
            </p>
          </div>
          <div className="tutor-mascot-box">
            <LinguaMascot mood={mascotMood} />
          </div>
        </div>

        {/* AI Quota Status Strip (G7) */}
        <div
          className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[#dfd9ca] bg-[#fffdf8] px-4 py-3 text-xs shadow-sm"
          data-testid="ai-quota-strip"
        >
          <div className="flex flex-wrap items-center gap-2.5">
            <span
              className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 font-bold ${
                isQuotaExhausted
                  ? "border border-[#e5b5a8] bg-[#fdf1ee] text-[#a33f28]"
                  : "border border-[#d3dcc1] bg-[#eff4e5] text-[#495734]"
              }`}
            >
              <Zap size={13} className="shrink-0" />
              {quota === null ? (
                <span>{t("Đang tải lượt AI...", "Loading AI turns...")}</span>
              ) : isGuestMode ? (
                <span>
                  {t(
                    `Khách dùng thử: còn ${quota.totalAvailable}/${quota.dailyFreeLimit} lượt hôm nay`,
                    `Guest trial: ${quota.totalAvailable}/${quota.dailyFreeLimit} turns left today`
                  )}
                </span>
              ) : (
                <span>
                  {t(
                    `Còn ${quota.totalAvailable} lượt hôm nay (${quota.dailyRemaining}/${quota.dailyFreeLimit} miễn phí · +${quota.bonusCredits} thưởng)`,
                    `${quota.totalAvailable} turns left today (${quota.dailyRemaining}/${quota.dailyFreeLimit} free · +${quota.bonusCredits} bonus)`
                  )}
                </span>
              )}
            </span>

            {quota !== null && !isGuestMode && (
              <span className="inline-flex items-center gap-1 text-[11px] font-medium text-[#6f7167]">
                <Clock size={12} />
                {t(
                  `Làm mới 00:00 GMT+7 · Thưởng hôm nay ${quota.dailyBonusEarned}/${quota.dailyBonusCap}`,
                  `Resets 00:00 GMT+7 · Bonus earned ${quota.dailyBonusEarned}/${quota.dailyBonusCap}`
                )}
              </span>
            )}
          </div>

          {isGuestMode ? (
            <Link
              href="/auth"
              className="inline-flex items-center gap-1.5 rounded-lg bg-[#69734f] px-3 py-1.5 text-[11px] font-bold text-[#fffefa] no-underline transition hover:bg-[#576140]"
            >
              <LogIn size={13} />
              {t("Đăng nhập Mezon (+5 lượt/ngày)", "Sign in with Mezon (5 turns/day)")}
            </Link>
          ) : (
            <div className="flex flex-wrap items-center gap-1.5">
              <Link
                href="/games"
                className="inline-flex items-center gap-1 rounded-lg border border-[#dbd5c6] bg-[#f7f4ec] px-2.5 py-1 text-[11px] font-semibold text-[#4c543a] no-underline hover:bg-[#edf1e4]"
              >
                <Gamepad2 size={12} /> {t("Game +1", "Game +1")}
              </Link>
              <Link
                href="/quiz"
                className="inline-flex items-center gap-1 rounded-lg border border-[#dbd5c6] bg-[#f7f4ec] px-2.5 py-1 text-[11px] font-semibold text-[#4c543a] no-underline hover:bg-[#edf1e4]"
              >
                <Brain size={12} /> {t("Quiz +1", "Quiz +1")}
              </Link>
              <Link
                href="/learn"
                className="inline-flex items-center gap-1 rounded-lg border border-[#dbd5c6] bg-[#f7f4ec] px-2.5 py-1 text-[11px] font-semibold text-[#4c543a] no-underline hover:bg-[#edf1e4]"
              >
                <BookOpen size={12} /> {t("Flashcard +1", "Flashcard +1")}
              </Link>
            </div>
          )}
        </div>

        {/* Tab Selector */}
        <div className="tutor-tabs">
          <button
            className={`tutor-tab-btn ${activeTab === "roleplay" ? "is-active" : ""}`}
            onClick={() => handleSelectTab("roleplay")}
          >
            <MessageSquare size={16} /> {t("Đóng vai Tình huống (Roleplay)", "Roleplay scenarios")}
          </button>
          <button
            className={`tutor-tab-btn ${activeTab === "grammar" ? "is-active" : ""}`}
            onClick={() => handleSelectTab("grammar")}
          >
            <HelpCircle size={16} /> {t("Kiểm tra & Sửa ngữ pháp (/ask)", "Check & fix grammar (/ask)")}
          </button>
        </div>
      </header>

      {/* Roleplay Scenarios Selection */}
      {activeTab === "roleplay" && (
        <section className="tutor-scenarios" aria-label={t("Danh mục kịch bản", "Scenario categories")}>
          {scenarios.map((sc) => {
            const ScIcon = SCENARIO_ICONS[sc.code] || MessageSquare;
            const isSelected = selectedScenario.code === sc.code;
            return (
              <button
                key={sc.code}
                className={`tutor-scenario-card ${isSelected ? "is-selected" : ""}`}
                onClick={() => handleSelectScenario(sc)}
              >
                <div className="scenario-card-header">
                  <span className="scenario-icon">
                    <ScIcon size={18} />
                  </span>
                  <span className="scenario-level">{sc.level}</span>
                </div>
                <h2 className="scenario-title">
                  {t(
                    sc.title,
                    sc.code === "cafe_order"
                      ? "London coffee shop"
                      : sc.code === "airport_checkin"
                        ? "International airport check-in"
                        : "Job interview"
                  )}
                </h2>
                <p className="scenario-desc">
                  {t(
                    sc.description,
                    sc.code === "cafe_order"
                      ? "Practice ordering drinks, choosing sizes and milk, and paying at a London barista counter."
                      : sc.code === "airport_checkin"
                        ? "Practice check-in, baggage, and departure gate questions at Heathrow Airport."
                        : "Practice answering interview questions about experience, strengths, and career goals."
                  )}
                </p>
                <span className="scenario-role">🎭 {sc.characterRole}</span>
              </button>
            );
          })}
        </section>
      )}

      {/* Main Chat Interface */}
      <main className="tutor-chat-card">
        {/* Chat Header */}
        <div className="chat-card-header">
          <div className="chat-role-info">
            <div className="chat-role-avatar">
              <Bot size={20} />
            </div>
            <div>
              <h3>{activeTab === "roleplay" ? selectedScenario.characterRole : "Mascot LingLing"}</h3>
              <p>
                {activeTab === "roleplay"
                  ? `${t("Đang trong tình huống", "Scenario")}: ${selectedScenario.title}`
                  : t("Chế độ cố vấn ngữ pháp thông minh", "Smart grammar coach mode")}
              </p>
            </div>
          </div>
          {activeTab === "roleplay" && (
            <button
              className="chat-reset-btn"
              onClick={resetSession}
              title={t("Bắt đầu lại phiên hội thoại", "Restart conversation")}
            >
              <RotateCcw size={15} /> {t("Làm mới", "Reset")}
            </button>
          )}
        </div>

        {/* Message Stream */}
        <div className="chat-messages-stream" ref={chatStreamRef}>
          {messages.map((msg) => (
            <div key={msg.id} className={`chat-bubble-row ${msg.sender === "user" ? "is-user" : "is-bot"}`}>
              {msg.sender === "bot" && (
                <div className="chat-avatar-mini" aria-hidden="true">
                  <Bot size={16} />
                </div>
              )}
              <div className="chat-bubble-content">
                <div className="chat-bubble-body">
                  <p className="chat-text">{msg.text}</p>
                </div>

                {/* AI Grammar Feedback Box */}
                {msg.hasFeedback && (msg.feedbackCorrection || msg.explanation) && (
                  <div className="chat-feedback-card">
                    <div className="feedback-header">
                      <Lightbulb size={16} />
                      <strong>{t("Gợi ý ngữ pháp từ LingLing", "Grammar tip from LingLing")}</strong>
                    </div>
                    {msg.feedbackCorrection && (
                      <p className="feedback-correction">
                        <CheckCircle2 size={14} /> {msg.feedbackCorrection}
                      </p>
                    )}
                    {msg.explanation && (
                      <p className="feedback-explanation">
                        <AlertCircle size={14} /> {msg.explanation}
                      </p>
                    )}
                    {msg.tips && (
                      <p className="feedback-tips">
                        💡 <em>Mẹo: {msg.tips}</em>
                      </p>
                    )}
                  </div>
                )}

                {msg.timestamp && <span className="chat-timestamp">{msg.timestamp}</span>}
              </div>
            </div>
          ))}

          {loading && (
            <div className="chat-bubble-row is-bot">
              <div className="chat-avatar-mini" aria-hidden="true">
                <Bot size={16} />
              </div>
              <div className="chat-bubble-content">
                <div className="chat-typing-indicator">
                  <span className="dot"></span>
                  <span className="dot"></span>
                  <span className="dot"></span>
                  <span className="typing-text">
                    {t("LingLing đang lắng nghe và suy nghĩ...", "LingLing is listening and thinking…")}
                  </span>
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Quota Exhausted Callout (G7) */}
        {(isQuotaExhausted || quotaErrorBanner) && (
          <div
            className="mx-4 mb-3 rounded-xl border border-[#e7c4b3] bg-[#fff7f2] p-4 text-sm text-[#4a352a]"
            role="status"
            data-testid="ai-quota-exhausted-card"
          >
            <div className="flex items-start gap-2.5">
              <AlertCircle size={18} className="mt-0.5 shrink-0 text-[#b44c32]" />
              <div className="flex-1 space-y-2">
                <p className="font-bold text-[#7c2e1b]">
                  {isGuestMode
                    ? t(
                        "Bạn đã dùng hết 2 lượt trải nghiệm miễn phí hôm nay!",
                        "You have used all 2 free guest turns today!"
                      )
                    : t(
                        "Bạn đã dùng hết lượt hỏi LingLing AI hôm nay!",
                        "You have used all your LingLing AI turns today!"
                      )}
                </p>
                <p className="text-xs leading-relaxed text-[#6b4f41]">
                  {quotaErrorBanner ??
                    (isGuestMode
                      ? t(
                          "Đăng nhập bằng tài khoản Mezon để nhận ngay 5 lượt miễn phí mỗi ngày (làm mới 00:00 GMT+7) và kiếm thêm tối đa +5 lượt thưởng/ngày từ Game Center, Quiz và Flashcard.",
                          "Sign in with Mezon to get 5 free turns daily (resets at 00:00 GMT+7) plus up to +5 bonus turns/day from Game Center, Quiz, and Flashcards."
                        )
                      : t(
                          "Kiếm thêm +1 lượt thưởng ngay lập tức bằng cách hoàn thành hoạt động học tập bên dưới (tối đa +5 lượt thưởng/ngày), hoặc quay lại vào 00:00 GMT+7 để nhận 5 lượt miễn phí mới.",
                          "Earn +1 bonus turn right away by completing any activity below (up to +5 bonus turns/day), or return at 00:00 GMT+7 for 5 fresh free turns."
                        ))}
                </p>

                {isGuestMode ? (
                  <div className="flex flex-wrap items-center gap-2 pt-1">
                    <Link
                      href="/auth"
                      className="inline-flex items-center gap-2 rounded-lg bg-[#69734f] px-4 py-2 text-xs font-bold text-[#fffefa] no-underline shadow-sm transition hover:bg-[#576140]"
                      data-testid="guest-login-cta"
                    >
                      <LogIn size={14} />
                      {t("Đăng nhập bằng Mezon ngay", "Sign in with Mezon now")}
                    </Link>
                  </div>
                ) : (
                  <div className="flex flex-wrap items-center gap-2 pt-1">
                    <Link
                      href="/games"
                      className="inline-flex items-center gap-1.5 rounded-lg bg-[#69734f] px-3 py-2 text-xs font-bold text-[#fffefa] no-underline transition hover:bg-[#576140]"
                    >
                      <Gamepad2 size={14} />
                      {t("Chơi Game Center (+1 · >=50%)", "Play Game Center (+1 · >=50%)")}
                    </Link>
                    <Link
                      href="/quiz"
                      className="inline-flex items-center gap-1.5 rounded-lg border border-[#d6d0c0] bg-[#fffefa] px-3 py-2 text-xs font-bold text-[#4a5437] no-underline transition hover:bg-[#f2f4ea]"
                    >
                      <Brain size={14} />
                      {t("Làm Quiz nhanh (+1 · >=60%)", "Take Quick Quiz (+1 · >=60%)")}
                    </Link>
                    <Link
                      href="/learn"
                      className="inline-flex items-center gap-1.5 rounded-lg border border-[#d6d0c0] bg-[#fffefa] px-3 py-2 text-xs font-bold text-[#4a5437] no-underline transition hover:bg-[#f2f4ea]"
                    >
                      <BookOpen size={14} />
                      {t("Ôn Flashcard (+1 · >=5 thẻ)", "Review Flashcards (+1 · >=5 cards)")}
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Quick Suggestion Prompts */}
        {activeTab === "roleplay" && QUICK_PROMPTS[selectedScenario.code] && (
          <div className="chat-quick-prompts">
            <span className="quick-prompt-label">{t("Gợi ý câu:", "Try saying:")}</span>
            {QUICK_PROMPTS[selectedScenario.code].map((prompt, i) => (
              <button
                key={i}
                className="quick-prompt-chip"
                onClick={() => void handleSendMessage(prompt)}
                disabled={loading || isQuotaExhausted}
              >
                <span>{prompt}</span>
                <ArrowRight size={12} />
              </button>
            ))}
          </div>
        )}

        {/* Input Bar */}
        <div className="chat-input-bar">
          <input
            type="text"
            className="chat-input-field"
            value={inputMessage}
            onChange={(e) => setInputMessage(e.target.value)}
            placeholder={
              isQuotaExhausted
                ? isGuestMode
                  ? t("Đăng nhập Mezon để tiếp tục trò chuyện cùng LingLing...", "Sign in with Mezon to continue chatting with LingLing...")
                  : t("Hết lượt AI hôm nay — kiếm thêm lượt qua Game/Quiz/Flashcard...", "Out of AI turns today — earn more via Game/Quiz/Flashcards...")
                : activeTab === "roleplay"
                  ? `${t("Nhập câu tiếng Anh để đối đáp với", "Enter an English sentence to reply to")} ${selectedScenario.characterRole}...`
                  : t("Nhập câu tiếng Anh bất kỳ để LingLing kiểm tra lỗi ngữ pháp...", "Enter any English sentence for LingLing to check...")
            }
            disabled={loading || isQuotaExhausted}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                void handleSendMessage();
              }
            }}
          />
          <button
            className="chat-send-btn"
            onClick={() => void handleSendMessage()}
            disabled={!inputMessage.trim() || loading || isQuotaExhausted}
          >
            <Send size={16} />
            <span>{t("Gửi", "Send")}</span>
          </button>
        </div>
      </main>
    </div>
  );
}
