-- ==============================================================================
-- Module 03: Learning, Spaced Repetition (SRS) & Gamification
-- Tinh gọn từ 8 bảng còn đúng 4 bảng tối ưu theo định hướng Mentor Mai Hồng Mận:
--  1. Gộp bảng learning_sessions vào cột session_history (JSONB) trong lesson_progress.
--  2. Gộp bảng srs_reviews vào cột review_history (JSONB) trong srs_cards.
--  3. Gộp bảng streak_freeze_events vào cột freeze_history (JSONB) trong user_streaks.
--  4. Gộp bảng user_daily_activity vào cột daily_activity (JSONB) trong user_streaks.
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 1. BẢNG TIẾN ĐỘ BÀI HỌC (Gộp lịch sử phiên học của bài)
CREATE TABLE IF NOT EXISTS lesson_progress (
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    lesson_id        UUID NOT NULL REFERENCES lessons(id) ON DELETE CASCADE,
    status           VARCHAR(16) NOT NULL DEFAULT 'not_started' 
                     CHECK (status IN ('not_started', 'in_progress', 'completed')),
    first_started_at TIMESTAMPTZ,
    completed_at     TIMESTAMPTZ,
    best_score       NUMERIC(5,2) CHECK (best_score BETWEEN 0 AND 100),
    -- Gộp lịch sử các phiên học bài này (thay cho learning_sessions):
    -- Ví dụ: [{"started_at": "...", "duration_seconds": 450, "score": 90}]
    session_history  JSONB NOT NULL DEFAULT '[]'::jsonb,
    PRIMARY KEY(user_id, lesson_id)
);

-- 2. BẢNG THẺ NHỚ SRS (Thuật toán SM-2 Spaced Repetition, gộp lịch sử lật thẻ)
CREATE TABLE IF NOT EXISTS srs_cards (
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    vocabulary_id    UUID NOT NULL REFERENCES vocabulary_items(id) ON DELETE CASCADE,
    repetitions      INTEGER NOT NULL DEFAULT 0 CHECK (repetitions >= 0),
    interval_days    NUMERIC(8,2) NOT NULL DEFAULT 0 CHECK (interval_days >= 0),
    ease_factor      NUMERIC(4,2) NOT NULL DEFAULT 2.50 CHECK (ease_factor >= 1.30),
    next_due_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_reviewed_at TIMESTAMPTZ,
    lapses           INTEGER NOT NULL DEFAULT 0 CHECK (lapses >= 0),
    suspended_at     TIMESTAMPTZ,
    -- Gộp lịch sử từng lần review thẻ (thay cho bảng srs_reviews):
    -- Ví dụ: [{"rating": "good", "interval_days": 3, "ease_factor": 2.5, "reviewed_at": "..."}]
    review_history   JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY(user_id, vocabulary_id)
);

-- 3. BẢNG SỔ CÁI ĐIỂM XP (Giữ nguyên tính năng chống cộng trùng lặp)
CREATE TABLE IF NOT EXISTS xp_ledger (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    source_type      VARCHAR(24) NOT NULL 
                     CHECK (source_type IN ('new_vocabulary','srs_review','clan_quiz','speed_bonus','lesson_complete','duel_win','duel_participation','adjustment')),
    source_id        UUID,
    xp_delta         INTEGER NOT NULL CHECK (xp_delta <> 0),
    awarded_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    idempotency_key  VARCHAR(200) UNIQUE,
    metadata         JSONB NOT NULL DEFAULT '{}'::jsonb
);

-- 4. BẢNG STREAK & HOẠT ĐỘNG (Gộp cả lịch sử vé freeze và hoạt động theo ngày)
CREATE TABLE IF NOT EXISTS user_streaks (
    user_id          UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    current_days     INTEGER NOT NULL DEFAULT 0 CHECK (current_days >= 0),
    best_days        INTEGER NOT NULL DEFAULT 0 CHECK (best_days >= 0),
    last_active_date DATE,
    freeze_balance   INTEGER NOT NULL DEFAULT 0 CHECK (freeze_balance >= 0),
    -- Gộp lịch sử nhận / dùng vé đóng băng (thay cho streak_freeze_events):
    -- Ví dụ: [{"event": "earned", "quantity": 1, "reason": "streak_7d", "date": "..."}]
    freeze_history   JSONB NOT NULL DEFAULT '[]'::jsonb,
    -- Gộp thống kê hoạt động theo ngày để vẽ Heatmap (thay cho user_daily_activity):
    -- Ví dụ: {"2026-10-07": {"xp": 60, "study_seconds": 1200, "words_count": 8}}
    daily_activity   JSONB NOT NULL DEFAULT '{}'::jsonb,
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Chỉ mục truy vấn
CREATE INDEX IF NOT EXISTS idx_srs_due_queue ON srs_cards(user_id, next_due_at) WHERE suspended_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_xp_ledger_user ON xp_ledger(user_id, awarded_at DESC);
CREATE INDEX IF NOT EXISTS idx_xp_ledger_source ON xp_ledger(source_type, source_id);

-- Chống cộng XP trùng lặp khi app không truyền idempotency_key
CREATE UNIQUE INDEX IF NOT EXISTS uq_xp_ledger_source
    ON xp_ledger(source_type, source_id) WHERE source_id IS NOT NULL;

-- Triggers tự động cập nhật updated_at
DROP TRIGGER IF EXISTS trg_srs_cards_updated_at ON srs_cards;
CREATE TRIGGER trg_srs_cards_updated_at
    BEFORE UPDATE ON srs_cards
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_user_streaks_updated_at ON user_streaks;
CREATE TRIGGER trg_user_streaks_updated_at
    BEFORE UPDATE ON user_streaks
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
