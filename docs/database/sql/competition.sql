-- ==============================================================================
-- Module 06: Competition — Word Duel & Leaderboard (Tinh gọn từ 7 bảng còn 2 bảng tối ưu)
-- PostgreSQL 13+. Tối ưu hóa:
--  1. Gộp duel_match_questions, duel_answers, duel_results vào bảng duy nhất duel_matches.
--  2. Gộp leaderboard_weeks, user_weekly_leaderboard, configured_clan_weekly_stats vào weekly_leaderboards.
-- ==============================================================================

-- 1. BẢNG TRẬN ĐẤU ĐỐI KHÁNG WORD DUEL (Gộp toàn bộ câu hỏi, bài làm 2 bên và kết quả)
CREATE TABLE IF NOT EXISTS duel_matches (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    challenger_id        UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    opponent_id          UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status               VARCHAR(16) NOT NULL DEFAULT 'pending' 
                         CHECK (status IN ('pending', 'accepted', 'in_progress', 'completed', 'declined', 'expired', 'cancelled')),
    question_count       SMALLINT NOT NULL DEFAULT 5 CHECK (question_count > 0),
    -- Danh sách ID các câu hỏi của trận đấu (thay cho duel_match_questions):
    question_ids         JSONB NOT NULL DEFAULT '[]'::jsonb,
    -- Chi tiết bài làm của người thách đấu (thay cho duel_answers):
    -- Ví dụ: [{"question_id": "...", "selected_key": "A", "is_correct": true, "response_ms": 1200}]
    challenger_answers   JSONB NOT NULL DEFAULT '[]'::jsonb,
    -- Chi tiết bài làm của đối thủ (thay cho duel_answers):
    opponent_answers     JSONB NOT NULL DEFAULT '[]'::jsonb,
    -- Kết quả trận đấu đưa thẳng vào bảng (thay cho duel_results):
    challenger_score     NUMERIC(8,2) NOT NULL DEFAULT 0,
    opponent_score       NUMERIC(8,2) NOT NULL DEFAULT 0,
    challenger_correct   SMALLINT NOT NULL DEFAULT 0,
    opponent_correct     SMALLINT NOT NULL DEFAULT 0,
    winner_id            UUID REFERENCES users(id) ON DELETE SET NULL,
    started_at           TIMESTAMPTZ,
    completed_at         TIMESTAMPTZ,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    CHECK (challenger_id <> opponent_id)
);

-- 2. BẢNG XẾP HẠNG TUẦN (Weekly Leaderboard tinh gọn)
CREATE TABLE IF NOT EXISTS weekly_leaderboards (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    week_start           DATE NOT NULL,
    week_end             DATE NOT NULL,
    user_id              UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    xp_total             INTEGER NOT NULL DEFAULT 0,
    rank                 SMALLINT CHECK (rank > 0),
    status               VARCHAR(16) NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'finalized')),
    finalized_at         TIMESTAMPTZ,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(week_start, user_id),
    CHECK (week_end >= week_start)
);

-- Chỉ mục tối ưu hóa truy vấn lịch sử đấu và bảng xếp hạng
CREATE INDEX IF NOT EXISTS idx_duel_challenger ON duel_matches(challenger_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_duel_opponent ON duel_matches(opponent_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_weekly_leaderboard_rank ON weekly_leaderboards(week_start, rank);
