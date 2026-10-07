-- ==============================================================================
-- Module 04: Quiz Engine (Tinh gọn từ 6 bảng còn 3 bảng theo định hướng Mentor Mai Hồng Mận)
-- PostgreSQL 13+. Tối ưu hóa:
--  1. Gộp bảng quiz_options vào cột options (JSONB) trong quiz_questions.
--  2. Bỏ bảng trung gian quiz_items: Gắn trực tiếp quiz_id, sort_order, points vào quiz_questions.
--  3. Gộp bảng quiz_attempt_answers vào cột answers_detail (JSONB) trong quiz_attempts.
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 1. BẢNG QUIZZES (Bộ đề thi / Bài trắc nghiệm)
CREATE TABLE IF NOT EXISTS quizzes (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code               VARCHAR(80) NOT NULL UNIQUE,
    title              VARCHAR(200) NOT NULL,
    description        TEXT,
    quiz_type          VARCHAR(20) NOT NULL CHECK (quiz_type IN ('practice', 'placement', 'lesson', 'clan')),
    level              VARCHAR(2) CHECK (level IN ('A1', 'A2', 'B1', 'B2')),
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    time_limit_seconds INTEGER CHECK (time_limit_seconds > 0),
    total_questions    SMALLINT NOT NULL DEFAULT 0,
    created_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. BẢNG QUIZ_QUESTIONS (Câu hỏi + Đáp án JSONB, liên kết trực tiếp với đề thi hoặc ngân hàng câu hỏi)
-- Thay thế cho 3 bảng cũ: quiz_questions, quiz_options, quiz_items
CREATE TABLE IF NOT EXISTS quiz_questions (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quiz_id            UUID REFERENCES quizzes(id) ON DELETE CASCADE, -- Gắn vào đề thi (NULL nếu là câu hỏi ngân hàng dùng chung)
    question_type      VARCHAR(24) NOT NULL CHECK (question_type IN ('multiple_choice', 'word_matching', 'sentence_scramble', 'spelling', 'dictation')),
    prompt             TEXT NOT NULL,
    prompt_audio_url   TEXT,
    difficulty         SMALLINT CHECK (difficulty BETWEEN 1 AND 5),
    level              VARCHAR(2) CHECK (level IN ('A1', 'A2', 'B1', 'B2')),
    -- Đáp án trắc nghiệm gộp vào JSONB:
    -- Ví dụ: [{"key": "A", "text": "Apple", "is_correct": true}, {"key": "B", "text": "Banana", "is_correct": false}]
    options            JSONB NOT NULL DEFAULT '[]'::jsonb,
    -- Payload động cho dạng bài ghép từ / xếp câu / âm thanh
    payload            JSONB NOT NULL DEFAULT '{}'::jsonb,
    explanation        TEXT,
    points             SMALLINT NOT NULL DEFAULT 1 CHECK (points > 0),      -- Đưa từ quiz_items sang
    sort_order         SMALLINT NOT NULL DEFAULT 1 CHECK (sort_order > 0),  -- Thứ tự câu trong đề thi
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    created_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. BẢNG QUIZ_ATTEMPTS (Lượt làm bài + Toàn bộ chi tiết câu trả lời)
-- Thay thế cho 2 bảng cũ: quiz_attempts và quiz_attempt_answers
CREATE TABLE IF NOT EXISTS quiz_attempts (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quiz_id            UUID NOT NULL REFERENCES quizzes(id) ON DELETE CASCADE,
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status             VARCHAR(16) NOT NULL DEFAULT 'in_progress' CHECK (status IN ('in_progress', 'submitted', 'expired', 'abandoned')),
    score              NUMERIC(7,2) DEFAULT 0,
    max_score          NUMERIC(7,2) DEFAULT 0,
    correct_count      SMALLINT DEFAULT 0,
    -- Toàn bộ chi tiết bài làm của học viên lưu vào JSONB:
    -- Ví dụ: [{"question_id": "...", "selected_key": "A", "is_correct": true, "points_awarded": 1, "answered_at": "..."}]
    answers_detail     JSONB NOT NULL DEFAULT '[]'::jsonb,
    started_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    submitted_at       TIMESTAMPTZ
);

-- Chỉ mục tối ưu hóa hiệu năng truy vấn
CREATE INDEX IF NOT EXISTS idx_quizzes_type_level ON quizzes(quiz_type, level, status);
CREATE INDEX IF NOT EXISTS idx_quiz_questions_quiz ON quiz_questions(quiz_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_quiz_questions_filter ON quiz_questions(status, level, question_type);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_user_time ON quiz_attempts(user_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_quiz ON quiz_attempts(quiz_id, status);

-- Chống tạo 2 attempt đang làm dở cùng lúc
CREATE UNIQUE INDEX IF NOT EXISTS uq_quiz_attempt_in_progress
    ON quiz_attempts(quiz_id, user_id) WHERE status = 'in_progress';

-- Triggers tự động cập nhật updated_at
DROP TRIGGER IF EXISTS trg_quizzes_updated_at ON quizzes;
CREATE TRIGGER trg_quizzes_updated_at
    BEFORE UPDATE ON quizzes
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_quiz_questions_updated_at ON quiz_questions;
CREATE TRIGGER trg_quiz_questions_updated_at
    BEFORE UPDATE ON quiz_questions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
