-- ==============================================================================
-- Module 05: Community Single-Clan Bot (Tinh gọn từ 5 bảng còn 2 bảng tối ưu)
-- PostgreSQL 13+. Mô hình Singleton Clan Bot kết nối Mezon Platform:
--  1. Gộp bảng bot_schedules vào cột schedules (JSONB) trong bot_configuration.
--  2. Bỏ bảng configured_clan_members & configured_clan_role_grants:
--     Học viên và quyền Clan Moderator đã được quản lý tập trung ở users (Simple RBAC).
--  3. Gộp bảng clan_quiz_responses vào cột responses (JSONB) trong clan_quiz_sessions.
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 1. BẢNG CẤU HÌNH BOT TRONG CLAN (Singleton id = 1, gộp cả lịch hẹn giờ tự động)
CREATE TABLE IF NOT EXISTS bot_configuration (
    id                       SMALLINT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
    mezon_clan_id            VARCHAR(128) NOT NULL UNIQUE,
    clan_name_snapshot       VARCHAR(200),
    default_channel_mezon_id VARCHAR(128),
    status                   VARCHAR(16) NOT NULL DEFAULT 'active' 
                             CHECK (status IN ('active', 'paused', 'uninstalled')),
    timezone                 VARCHAR(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    installed_by             UUID REFERENCES users(id) ON DELETE SET NULL,
    installed_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
    uninstalled_at           TIMESTAMPTZ,
    daily_word_enabled       BOOLEAN NOT NULL DEFAULT true,
    quiz_enabled             BOOLEAN NOT NULL DEFAULT true,
    quiz_duration_seconds    SMALLINT NOT NULL DEFAULT 30 CHECK (quiz_duration_seconds BETWEEN 5 AND 300),
    -- Gộp toàn bộ lịch trình tự động vào JSONB (thay cho bảng bot_schedules):
    -- Ví dụ: [
    --   {"type": "word_of_day", "channel_id": "...", "time": "08:00", "weekdays": [0,1,2,3,4,5,6], "enabled": true, "next_run_at": "..."},
    --   {"type": "daily_quiz",  "channel_id": "...", "time": "20:00", "weekdays": [1,2,3,4,5],     "enabled": true, "next_run_at": "..."}
    -- ]
    schedules                JSONB NOT NULL DEFAULT '[]'::jsonb,
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. BẢNG PHIÊN ĐỐ VUI CLAN QUIZ (Gộp toàn bộ câu trả lời của các thành viên)
CREATE TABLE IF NOT EXISTS clan_quiz_sessions (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_mezon_id         VARCHAR(128) NOT NULL,
    question_id              UUID NOT NULL REFERENCES quiz_questions(id) ON DELETE RESTRICT,
    started_by               UUID REFERENCES users(id) ON DELETE SET NULL,
    status                   VARCHAR(16) NOT NULL DEFAULT 'open' 
                             CHECK (status IN ('open', 'closed', 'cancelled')),
    opened_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    closes_at                TIMESTAMPTZ NOT NULL,
    closed_at                TIMESTAMPTZ,
    winning_user_id          UUID REFERENCES users(id) ON DELETE SET NULL,
    winning_response_ms      INTEGER CHECK (winning_response_ms >= 0),
    explanation_published_at TIMESTAMPTZ,
    -- Gộp toàn bộ câu trả lời của các thành viên vào JSONB (thay cho bảng clan_quiz_responses):
    -- Ví dụ: [{"user_id": "...", "selected_key": "A", "is_correct": true, "response_ms": 1420, "responded_at": "..."}]
    responses                JSONB NOT NULL DEFAULT '[]'::jsonb,
    CHECK (closes_at > opened_at)
);

-- 3. BẢNG CLAN_MODERATOR_GRANTS (Quyền vận hành phạm vi 1 Clan, tách biệt role toàn cục)
CREATE TABLE IF NOT EXISTS clan_moderator_grants (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    granted_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    granted_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at         TIMESTAMPTZ,
    revoked_at         TIMESTAMPTZ,
    revoked_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    reason             TEXT,
    CHECK (expires_at IS NULL OR expires_at > granted_at),
    CHECK (revoked_at IS NULL OR revoked_at >= granted_at)
);

-- Chỉ mục tối ưu hóa truy vấn các phiên đố vui đang mở và quyền hạn
CREATE INDEX IF NOT EXISTS idx_clan_quiz_open ON clan_quiz_sessions(closes_at) WHERE status = 'open';
CREATE INDEX IF NOT EXISTS idx_clan_quiz_channel ON clan_quiz_sessions(channel_mezon_id, opened_at DESC);
CREATE INDEX IF NOT EXISTS idx_clan_quiz_question ON clan_quiz_sessions(question_id);
CREATE INDEX IF NOT EXISTS idx_clan_mod_grants_user ON clan_moderator_grants(user_id) WHERE revoked_at IS NULL;

-- Trigger tự động cập nhật updated_at
DROP TRIGGER IF EXISTS trg_bot_configuration_updated_at ON bot_configuration;
CREATE TRIGGER trg_bot_configuration_updated_at
    BEFORE UPDATE ON bot_configuration
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
