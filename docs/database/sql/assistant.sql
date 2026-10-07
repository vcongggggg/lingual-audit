-- ==============================================================================
-- Module 07: AI Assistant — LingLing Tutor (Tinh gọn từ 3 bảng còn 2 bảng tối ưu)
-- PostgreSQL 13+. Mô hình hội thoại AI chuẩn payload OpenAI/Gemini:
--  1. Giữ ai_scenarios cho danh mục kịch bản luyện tập / roleplay.
--  2. Gộp toàn bộ tin nhắn ai_messages vào cột messages (JSONB) trong ai_conversations.
-- ==============================================================================

-- 1. BẢNG KỊCH BẢN LUYỆN TẬP VỚI AI
CREATE TABLE IF NOT EXISTS ai_scenarios (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code               VARCHAR(80) NOT NULL UNIQUE,
    title              VARCHAR(160) NOT NULL,
    description        TEXT,
    level              VARCHAR(2) CHECK (level IN ('A1', 'A2', 'B1', 'B2')),
    system_prompt_key  VARCHAR(120) NOT NULL,
    status             VARCHAR(16) NOT NULL DEFAULT 'published' 
                       CHECK (status IN ('draft', 'published', 'archived')),
    created_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. BẢNG HỘI THOẠI AI (Gộp toàn bộ chuỗi tin nhắn vào JSONB)
CREATE TABLE IF NOT EXISTS ai_conversations (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    scenario_id        UUID REFERENCES ai_scenarios(id) ON DELETE SET NULL,
    conversation_type  VARCHAR(16) NOT NULL 
                       CHECK (conversation_type IN ('correction', 'roleplay', 'free_chat')),
    title              VARCHAR(200),
    status             VARCHAR(16) NOT NULL DEFAULT 'active' 
                       CHECK (status IN ('active', 'completed', 'archived')),
    -- Toàn bộ chuỗi tin nhắn lưu vào JSONB (thay cho bảng ai_messages):
    -- Chuẩn payload: [{"role": "user", "content": "..."}, {"role": "assistant", "content": "...", "correction": "..."}]
    messages           JSONB NOT NULL DEFAULT '[]'::jsonb,
    model_name         VARCHAR(80),
    total_tokens       INTEGER DEFAULT 0 CHECK (total_tokens >= 0),
    started_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at           TIMESTAMPTZ
);

-- Chỉ mục tối ưu hóa lịch sử trò chuyện của học viên
CREATE INDEX IF NOT EXISTS idx_ai_conversations_user ON ai_conversations(user_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_scenarios_level ON ai_scenarios(level, status);
