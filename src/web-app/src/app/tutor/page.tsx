"use client";

import { useEffect, useRef, useState } from "react";
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
  ArrowRight
} from "lucide-react";

interface Scenario {
  id: string;
  code: string;
  title: string;
  description: string;
  level: string;
  characterRole: string;
  greetingMessage: string;
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

export default function TutorPage() {
  const [scenarios, setScenarios] = useState<Scenario[]>(DEFAULT_SCENARIOS);
  const [selectedScenario, setSelectedScenario] = useState<Scenario>(DEFAULT_SCENARIOS[0]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [mascotMood, setMascotMood] = useState<MascotMood>("idle");
  const [activeTab, setActiveTab] = useState<"roleplay" | "grammar">("roleplay");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Load scenarios on mount
  useEffect(() => {
    async function loadScenarios() {
      try {
        const res = await fetch("/api/ai/scenarios");
        if (res.ok) {
          const data = (await res.json()) as Scenario[];
          if (Array.isArray(data) && data.length > 0) {
            setScenarios(data);
            setSelectedScenario(data[0]);
          }
        }
      } catch {
        // Fallback already initialized
      }
    }
    void loadScenarios();
  }, []);

  // Initialize or reset conversation when scenario changes
  useEffect(() => {
    if (activeTab === "roleplay" && selectedScenario) {
      setMessages([
        {
          id: "greeting-" + selectedScenario.code,
          sender: "bot",
          text: selectedScenario.greetingMessage,
          timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        },
      ]);
      setConversationId(null);
      setMascotMood("idle");
    } else if (activeTab === "grammar") {
      setMessages([
        {
          id: "grammar-intro",
          sender: "bot",
          text: "Xin chào! Mình là Mascot LingLing. Bạn hãy nhập bất kỳ câu tiếng Anh nào vào khung bên dưới, mình sẽ phân tích ngữ pháp, chỉ ra lỗi sai và hướng dẫn cách diễn đạt chuẩn bản xứ nhé!",
          timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        },
      ]);
      setMascotMood("idle");
    }
  }, [selectedScenario, activeTab]);

  // Auto scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, loading]);

  const handleSendMessage = async (textToSend?: string) => {
    const text = (textToSend ?? inputMessage).trim();
    if (!text || loading) return;

    setInputMessage("");
    setLoading(true);
    setMascotMood("thinking");

    const userMsg: ChatMessage = {
      id: "user-" + Date.now(),
      sender: "user",
      text: text,
      timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
    };

    setMessages((prev) => [...prev, userMsg]);

    try {
      if (activeTab === "roleplay") {
        const response = await fetch("/api/ai/roleplay", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            scenarioCode: selectedScenario.code,
            userMessage: text,
            conversationId: conversationId,
            mezonUserId: "web_user_demo",
          }),
        });

        if (!response.ok) throw new Error("Roleplay turn failed");

        const data = (await response.json()) as {
          conversationId: string;
          reply: string;
          hasFeedback: boolean;
          feedbackCorrection?: string | null;
          explanation?: string | null;
        };

        if (data.conversationId) {
          setConversationId(data.conversationId);
        }

        const botMsg: ChatMessage = {
          id: "bot-" + Date.now(),
          sender: "bot",
          text: data.reply,
          timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
          hasFeedback: data.hasFeedback,
          feedbackCorrection: data.feedbackCorrection,
          explanation: data.explanation,
        };

        setMessages((prev) => [...prev, botMsg]);
        setMascotMood(data.hasFeedback ? "encourage" : "correct");
      } else {
        // Grammar check mode
        const response = await fetch("/api/ai/grammar-check", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ text }),
        });

        if (!response.ok) throw new Error("Grammar check failed");

        const data = (await response.json()) as {
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
          ? "🎉 Tuyệt vời! Câu của bạn hoàn toàn chính xác về ngữ pháp và cách dùng từ tự nhiên."
          : "💡 LingLing đã kiểm tra câu của bạn. Có một số điểm cần lưu ý để câu tự nhiên hơn!";

        const botMsg: ChatMessage = {
          id: "bot-" + Date.now(),
          sender: "bot",
          text: botReply,
          timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
          hasFeedback: !data.isCorrect,
          feedbackCorrection: data.correctedText ? `Nên nói: "${data.correctedText}"` : null,
          explanation: data.explanation,
          tips: data.tips,
        };

        setMessages((prev) => [...prev, botMsg]);
        setMascotMood(data.isCorrect ? "correct" : "encourage");
      }
    } catch {
      const errorMsgText = activeTab === "grammar"
        ? "⚠️ Không thể kết nối đến máy chủ AI để kiểm tra ngữ pháp lúc này. Vui lòng thử lại sau ít phút."
        : "⚠️ Kết nối đến máy chủ AI tạm thời gián đoạn. Vui lòng thử lại sau ít phút.";

      const fallbackMsg: ChatMessage = {
        id: "bot-" + Date.now(),
        sender: "bot",
        text: errorMsgText,
        timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
      };
      setMessages((prev) => [...prev, fallbackMsg]);
      setMascotMood("encourage");
    } finally {
      setLoading(false);
    }
  };

  const resetSession = () => {
    setConversationId(null);
    setMessages([
      {
        id: "greeting-" + selectedScenario.code,
        sender: "bot",
        text: selectedScenario.greetingMessage,
        timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
      },
    ]);
    setMascotMood("idle");
  };

  const IconComponent = SCENARIO_ICONS[selectedScenario.code] || MessageSquare;

  return (
    <div className="tutor-container">
      {/* Header Banner */}
      <header className="tutor-header">
        <div className="tutor-header-main">
          <div className="tutor-title-box">
            <span className="tutor-badge">
              <Sparkles size={14} /> AI TUTOR • GOOGLE GEMINI
            </span>
            <h1 className="tutor-title">Luyện Giao Tiếp cùng Mascot LingLing</h1>
            <p className="tutor-desc">
              Thực hành đàm thoại tiếng Anh theo ngữ cảnh thực tế, nhận phản hồi ngữ pháp tức thì và nâng cao phản xạ tự nhiên.
            </p>
          </div>
          <div className="tutor-mascot-box">
            <LinguaMascot mood={mascotMood} />
          </div>
        </div>

        {/* Tab Selector */}
        <div className="tutor-tabs">
          <button
            className={`tutor-tab-btn ${activeTab === "roleplay" ? "is-active" : ""}`}
            onClick={() => setActiveTab("roleplay")}
          >
            <MessageSquare size={16} /> Đóng vai Tình huống (Roleplay)
          </button>
          <button
            className={`tutor-tab-btn ${activeTab === "grammar" ? "is-active" : ""}`}
            onClick={() => setActiveTab("grammar")}
          >
            <HelpCircle size={16} /> Kiểm tra & Sửa ngữ pháp (/ask)
          </button>
        </div>
      </header>

      {/* Roleplay Scenarios Selection */}
      {activeTab === "roleplay" && (
        <section className="tutor-scenarios" aria-label="Danh mục kịch bản">
          {scenarios.map((sc) => {
            const ScIcon = SCENARIO_ICONS[sc.code] || MessageSquare;
            const isSelected = selectedScenario.code === sc.code;
            return (
              <button
                key={sc.code}
                className={`tutor-scenario-card ${isSelected ? "is-selected" : ""}`}
                onClick={() => setSelectedScenario(sc)}
              >
                <div className="scenario-card-header">
                  <span className="scenario-icon">
                    <ScIcon size={18} />
                  </span>
                  <span className="scenario-level">{sc.level}</span>
                </div>
                <h2 className="scenario-title">{sc.title}</h2>
                <p className="scenario-desc">{sc.description}</p>
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
              <h3>
                {activeTab === "roleplay" ? selectedScenario.characterRole : "Mascot LingLing"}
              </h3>
              <p>
                {activeTab === "roleplay"
                  ? `Đang trong tình huống: ${selectedScenario.title}`
                  : "Chế độ cố vấn ngữ pháp thông minh"}
              </p>
            </div>
          </div>
          {activeTab === "roleplay" && (
            <button className="chat-reset-btn" onClick={resetSession} title="Bắt đầu lại phiên hội thoại">
              <RotateCcw size={15} /> Làm mới
            </button>
          )}
        </div>

        {/* Message Stream */}
        <div className="chat-messages-stream">
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
                      <strong>Gợi ý ngữ pháp từ LingLing</strong>
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

                <span className="chat-timestamp">{msg.timestamp}</span>
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
                  <span className="typing-text">LingLing đang lắng nghe và suy nghĩ...</span>
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Quick Suggestion Prompts */}
        {activeTab === "roleplay" && QUICK_PROMPTS[selectedScenario.code] && (
          <div className="chat-quick-prompts">
            <span className="quick-prompt-label">Gợi ý câu:</span>
            {QUICK_PROMPTS[selectedScenario.code].map((prompt, i) => (
              <button
                key={i}
                className="quick-prompt-chip"
                onClick={() => handleSendMessage(prompt)}
                disabled={loading}
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
              activeTab === "roleplay"
                ? `Nhập câu tiếng Anh để đối đáp với ${selectedScenario.characterRole}...`
                : "Nhập câu tiếng Anh bất kỳ để LingLing kiểm tra lỗi ngữ pháp..."
            }
            disabled={loading}
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
            disabled={!inputMessage.trim() || loading}
          >
            <Send size={16} />
            <span>Gửi</span>
          </button>
        </div>
      </main>
    </div>
  );
}

