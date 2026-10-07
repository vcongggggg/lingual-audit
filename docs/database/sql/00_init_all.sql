-- ==========================================================================
-- LINGUAL DATABASE — ALL-IN-ONE INITIALIZATION SCRIPT (PostgreSQL 13+)
-- Combined 9 Modules · 22 Tables · Single-Clan Mezon Architecture
-- Generated for Mezon Campus Studio 2026 (MCS 2026)
-- ==========================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Hàm tự động cập nhật timestamp updated_at khi có UPDATE
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- ==========================================================================
-- MODULE 01: USER.SQL
-- ==========================================================================

-- Module 01: Identity, Learner Profile & Placement Test (Tinh gọn theo định hướng Mentor Mai Hồng Mận)
-- PostgreSQL 13+. Mezon IDs are external identifiers; never store OAuth tokens here.

-- 1. BẢNG USERS (Tích hợp Simple RBAC trực tiếp qua cột role)
CREATE TABLE IF NOT EXISTS users (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mezon_user_id      VARCHAR(128) NOT NULL UNIQUE,
    display_name       VARCHAR(160) NOT NULL,
    avatar_url         TEXT,
    preferred_locale   VARCHAR(16) NOT NULL DEFAULT 'vi-VN',
    timezone           VARCHAR(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    status             VARCHAR(20) NOT NULL DEFAULT 'active'
                       CHECK (status IN ('active', 'suspended', 'deleted')),
    -- Simple RBAC: Phân quyền trực tiếp qua cột role (thay thế 2 bảng roles & user_roles)
    role               VARCHAR(20) NOT NULL DEFAULT 'learner'
                       CHECK (role IN ('learner', 'moderator', 'admin')),
    last_seen_at       TIMESTAMPTZ,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at         TIMESTAMPTZ
);

-- 2. BẢNG LEARNER_PROFILES (Hồ sơ học tập của học viên)
CREATE TABLE IF NOT EXISTS learner_profiles (
    user_id            UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    learning_goal      VARCHAR(32) CHECK (learning_goal IN ('daily_communication', 'career', 'certification')),
    daily_commitment_minutes SMALLINT CHECK (daily_commitment_minutes IN (5, 15, 30)),
    proficiency_level  VARCHAR(2) CHECK (proficiency_level IN ('A1', 'A2', 'B1', 'B2')),
    placement_status   VARCHAR(16) NOT NULL DEFAULT 'pending'
                       CHECK (placement_status IN ('pending', 'in_progress', 'completed', 'skipped', 'self_selected')),
    onboarding_completed_at TIMESTAMPTZ,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. BẢNG PLACEMENT_TESTS (Gộp cả lượt thi và chi tiết câu trả lời vào JSONB)
CREATE TABLE IF NOT EXISTS placement_tests (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status             VARCHAR(16) NOT NULL DEFAULT 'in_progress'
                       CHECK (status IN ('in_progress', 'completed', 'abandoned')),
    question_count     SMALLINT NOT NULL DEFAULT 10 CHECK (question_count > 0),
    correct_count      SMALLINT CHECK (correct_count >= 0 AND correct_count <= question_count),
    assessed_level     VARCHAR(2) CHECK (assessed_level IN ('A1', 'A2', 'B1', 'B2')),
    -- Chi tiết bài làm lưu dạng JSONB: [{"question_id": "...", "order": 1, "selected_option_id": "...", "is_correct": true}]
    answers_detail     JSONB NOT NULL DEFAULT '[]'::jsonb,
    started_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at       TIMESTAMPTZ
);

-- Chỉ mục tối ưu hóa Module 01
CREATE INDEX IF NOT EXISTS idx_users_mezon_id ON users(mezon_user_id);
CREATE INDEX IF NOT EXISTS idx_placement_tests_user ON placement_tests(user_id, started_at DESC);

-- Triggers tự động cập nhật updated_at cho Module 01
DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_learner_profiles_updated_at ON learner_profiles;
CREATE TRIGGER trg_learner_profiles_updated_at
    BEFORE UPDATE ON learner_profiles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 02: CURRICULUM.SQL (Tinh gọn từ 7 bảng còn 4 bảng theo Phương án A)
-- ==========================================================================

-- 1. BẢNG COURSES (Khóa học theo chuẩn CEFR A1-B2)
CREATE TABLE IF NOT EXISTS courses (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code               VARCHAR(80) NOT NULL UNIQUE,
    title              VARCHAR(200) NOT NULL,
    description        TEXT,
    source_language    VARCHAR(16) NOT NULL DEFAULT 'en',
    target_language    VARCHAR(16) NOT NULL DEFAULT 'vi',
    level              VARCHAR(2) NOT NULL CHECK (level IN ('A1', 'A2', 'B1', 'B2')),
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    sort_order         INTEGER NOT NULL DEFAULT 0,
    created_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. BẢNG UNITS (Chương / Chủ đề bài học trong khóa)
CREATE TABLE IF NOT EXISTS units (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id          UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    code               VARCHAR(80) NOT NULL,
    title              VARCHAR(200) NOT NULL,
    description        TEXT,
    topic              VARCHAR(120),
    sort_order         INTEGER NOT NULL DEFAULT 0,
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    UNIQUE(course_id, code),
    UNIQUE(course_id, sort_order)
);

-- 3. BẢNG LESSONS (Bài học + Danh sách ID từ vựng JSONB, thay thế bảng lesson_vocabulary)
CREATE TABLE IF NOT EXISTS lessons (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    unit_id            UUID NOT NULL REFERENCES units(id) ON DELETE CASCADE,
    code               VARCHAR(80) NOT NULL,
    title              VARCHAR(200) NOT NULL,
    summary            TEXT,
    estimated_minutes  SMALLINT CHECK (estimated_minutes > 0),
    sort_order         INTEGER NOT NULL DEFAULT 0,
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    vocabulary_ids     JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(unit_id, code),
    UNIQUE(unit_id, sort_order)
);

-- 4. BẢNG VOCABULARY_ITEMS (Kho từ vựng + Ví dụ câu JSONB, thay thế bảng vocabulary_examples)
CREATE TABLE IF NOT EXISTS vocabulary_items (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    term                    VARCHAR(240) NOT NULL,
    normalized_term         VARCHAR(240) NOT NULL,
    source_language         VARCHAR(16) NOT NULL DEFAULT 'en',
    target_language         VARCHAR(16) NOT NULL DEFAULT 'vi',
    ipa                     VARCHAR(240),
    part_of_speech          VARCHAR(32),
    meaning                 TEXT NOT NULL,
    pronunciation_audio_url TEXT,
    image_url               TEXT,
    examples                JSONB NOT NULL DEFAULT '[]'::jsonb,
    status                  VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    created_by              UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(normalized_term, source_language, target_language)
);

-- Chỉ mục tối ưu hóa Module 02
CREATE INDEX IF NOT EXISTS idx_units_course ON units(course_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_lessons_unit ON lessons(unit_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_vocabulary_search ON vocabulary_items(normalized_term, status);
CREATE INDEX IF NOT EXISTS idx_vocabulary_lang ON vocabulary_items(source_language, target_language);

-- Triggers tự động cập nhật updated_at cho Module 02
DROP TRIGGER IF EXISTS trg_courses_updated_at ON courses;
CREATE TRIGGER trg_courses_updated_at
    BEFORE UPDATE ON courses
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_lessons_updated_at ON lessons;
CREATE TRIGGER trg_lessons_updated_at
    BEFORE UPDATE ON lessons
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_vocabulary_items_updated_at ON vocabulary_items;
CREATE TRIGGER trg_vocabulary_items_updated_at
    BEFORE UPDATE ON vocabulary_items
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 03: LEARNING.SQL (Gộp từ 8 bảng còn 4 bảng tối ưu)
-- ==========================================================================

-- 1. BẢNG TIẾN ĐỘ BÀI HỌC (Gộp lịch sử phiên học của bài)
CREATE TABLE IF NOT EXISTS lesson_progress (
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    lesson_id        UUID NOT NULL REFERENCES lessons(id) ON DELETE CASCADE,
    status           VARCHAR(16) NOT NULL DEFAULT 'not_started' 
                     CHECK (status IN ('not_started', 'in_progress', 'completed')),
    first_started_at TIMESTAMPTZ,
    completed_at     TIMESTAMPTZ,
    best_score       NUMERIC(5,2) CHECK (best_score BETWEEN 0 AND 100),
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
    freeze_history   JSONB NOT NULL DEFAULT '[]'::jsonb,
    daily_activity   JSONB NOT NULL DEFAULT '{}'::jsonb,
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Chỉ mục tối ưu hóa Module 03
CREATE INDEX IF NOT EXISTS idx_srs_due_queue ON srs_cards(user_id, next_due_at) WHERE suspended_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_xp_ledger_user ON xp_ledger(user_id, awarded_at DESC);
CREATE INDEX IF NOT EXISTS idx_xp_ledger_source ON xp_ledger(source_type, source_id);

-- Chống cộng XP trùng lặp khi app không truyền idempotency_key
CREATE UNIQUE INDEX IF NOT EXISTS uq_xp_ledger_source
    ON xp_ledger(source_type, source_id) WHERE source_id IS NOT NULL;

-- Triggers tự động cập nhật updated_at cho Module 03
DROP TRIGGER IF EXISTS trg_srs_cards_updated_at ON srs_cards;
CREATE TRIGGER trg_srs_cards_updated_at
    BEFORE UPDATE ON srs_cards
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_user_streaks_updated_at ON user_streaks;
CREATE TRIGGER trg_user_streaks_updated_at
    BEFORE UPDATE ON user_streaks
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 04: QUIZ.SQL (Tinh gọn từ 6 bảng còn 3 bảng)
-- ==========================================================================

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

-- 2. BẢNG QUIZ_QUESTIONS (Câu hỏi + Đáp án JSONB)
CREATE TABLE IF NOT EXISTS quiz_questions (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quiz_id            UUID REFERENCES quizzes(id) ON DELETE CASCADE,
    question_type      VARCHAR(24) NOT NULL CHECK (question_type IN ('multiple_choice', 'word_matching', 'sentence_scramble', 'spelling', 'dictation')),
    prompt             TEXT NOT NULL,
    prompt_audio_url   TEXT,
    difficulty         SMALLINT CHECK (difficulty BETWEEN 1 AND 5),
    level              VARCHAR(2) CHECK (level IN ('A1', 'A2', 'B1', 'B2')),
    options            JSONB NOT NULL DEFAULT '[]'::jsonb,
    payload            JSONB NOT NULL DEFAULT '{}'::jsonb,
    explanation        TEXT,
    points             SMALLINT NOT NULL DEFAULT 1 CHECK (points > 0),
    sort_order         SMALLINT NOT NULL DEFAULT 1 CHECK (sort_order > 0),
    status             VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    created_by         UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. BẢNG QUIZ_ATTEMPTS (Lượt làm bài + Chi tiết câu trả lời JSONB)
CREATE TABLE IF NOT EXISTS quiz_attempts (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quiz_id            UUID NOT NULL REFERENCES quizzes(id) ON DELETE CASCADE,
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status             VARCHAR(16) NOT NULL DEFAULT 'in_progress' CHECK (status IN ('in_progress', 'submitted', 'expired', 'abandoned')),
    score              NUMERIC(7,2) DEFAULT 0,
    max_score          NUMERIC(7,2) DEFAULT 0,
    correct_count      SMALLINT DEFAULT 0,
    answers_detail     JSONB NOT NULL DEFAULT '[]'::jsonb,
    started_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    submitted_at       TIMESTAMPTZ
);

-- Chỉ mục tối ưu hóa Module 04
CREATE INDEX IF NOT EXISTS idx_quizzes_type_level ON quizzes(quiz_type, level, status);
CREATE INDEX IF NOT EXISTS idx_quiz_questions_quiz ON quiz_questions(quiz_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_quiz_questions_filter ON quiz_questions(status, level, question_type);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_user_time ON quiz_attempts(user_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_quiz ON quiz_attempts(quiz_id, status);

-- Chống tạo 2 attempt đang làm dở cùng lúc
CREATE UNIQUE INDEX IF NOT EXISTS uq_quiz_attempt_in_progress
    ON quiz_attempts(quiz_id, user_id) WHERE status = 'in_progress';

-- Triggers tự động cập nhật updated_at cho Module 04
DROP TRIGGER IF EXISTS trg_quizzes_updated_at ON quizzes;
CREATE TRIGGER trg_quizzes_updated_at
    BEFORE UPDATE ON quizzes
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_quiz_questions_updated_at ON quiz_questions;
CREATE TRIGGER trg_quiz_questions_updated_at
    BEFORE UPDATE ON quiz_questions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 05: COMMUNITY.SQL (Mô hình Singleton Clan Bot & Clan Quiz)
-- ==========================================================================

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
    responses                JSONB NOT NULL DEFAULT '[]'::jsonb
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

-- Chỉ mục tối ưu hóa Module 05
CREATE INDEX IF NOT EXISTS idx_clan_quiz_open ON clan_quiz_sessions(closes_at) WHERE status = 'open';
CREATE INDEX IF NOT EXISTS idx_clan_quiz_channel ON clan_quiz_sessions(channel_mezon_id, opened_at DESC);
CREATE INDEX IF NOT EXISTS idx_clan_quiz_question ON clan_quiz_sessions(question_id);
CREATE INDEX IF NOT EXISTS idx_clan_mod_grants_user ON clan_moderator_grants(user_id) WHERE revoked_at IS NULL;

-- Trigger tự động cập nhật updated_at cho Module 05
DROP TRIGGER IF EXISTS trg_bot_configuration_updated_at ON bot_configuration;
CREATE TRIGGER trg_bot_configuration_updated_at
    BEFORE UPDATE ON bot_configuration
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 06: COMPETITION.SQL
-- ==========================================================================

-- 1. BẢNG TRẬN ĐẤU ĐỐI KHÁNG WORD DUEL (Gộp toàn bộ câu hỏi, bài làm 2 bên và kết quả)
CREATE TABLE IF NOT EXISTS duel_matches (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    challenger_id        UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    opponent_id          UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status               VARCHAR(16) NOT NULL DEFAULT 'pending' 
                         CHECK (status IN ('pending', 'accepted', 'in_progress', 'completed', 'declined', 'expired', 'cancelled')),
    question_count       SMALLINT NOT NULL DEFAULT 5 CHECK (question_count > 0),
    question_ids         JSONB NOT NULL DEFAULT '[]'::jsonb,
    challenger_answers   JSONB NOT NULL DEFAULT '[]'::jsonb,
    opponent_answers     JSONB NOT NULL DEFAULT '[]'::jsonb,
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

-- Chỉ mục tối ưu hóa Module 06
CREATE INDEX IF NOT EXISTS idx_duel_challenger ON duel_matches(challenger_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_duel_opponent ON duel_matches(opponent_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_duel_winner ON duel_matches(winner_id);
CREATE INDEX IF NOT EXISTS idx_weekly_leaderboard_rank ON weekly_leaderboards(week_start, rank);
CREATE INDEX IF NOT EXISTS idx_weekly_lb_xp ON weekly_leaderboards(week_start, xp_total DESC);


-- ==========================================================================
-- MODULE 07: ASSISTANT.SQL (AI LingLing Roleplay & Correction)
-- ==========================================================================

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
    messages           JSONB NOT NULL DEFAULT '[]'::jsonb,
    model_name         VARCHAR(80),
    total_tokens       INTEGER DEFAULT 0 CHECK (total_tokens >= 0),
    started_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at           TIMESTAMPTZ
);

-- Chỉ mục tối ưu hóa Module 07
CREATE INDEX IF NOT EXISTS idx_ai_conversations_user ON ai_conversations(user_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_conv_scenario ON ai_conversations(scenario_id);
CREATE INDEX IF NOT EXISTS idx_ai_scenarios_level ON ai_scenarios(level, status);

-- Trigger tự động cập nhật updated_at cho Module 07
DROP TRIGGER IF EXISTS trg_ai_scenarios_updated_at ON ai_scenarios;
CREATE TRIGGER trg_ai_scenarios_updated_at
    BEFORE UPDATE ON ai_scenarios
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- ==========================================================================
-- MODULE 08: ANALYTICS.SQL (Phase 2 / Post-MVP)
-- Các chỉ số phân tích Cohort và báo cáo Clan được query trực tiếp từ xp_ledger và lesson_progress.
-- ==========================================================================


-- ==========================================================================
-- MODULE 09: AUDIT.SQL (Nhật ký thao tác nhạy cảm append-only cho Admin & Mod)
-- ==========================================================================

-- 1. BẢNG AUDIT_LOGS
CREATE TABLE IF NOT EXISTS audit_logs (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id           UUID REFERENCES users(id) ON DELETE SET NULL,
    action             VARCHAR(60) NOT NULL,
    entity_type        VARCHAR(60) NOT NULL,
    entity_id          UUID,
    reason             TEXT,
    metadata           JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Chỉ mục tối ưu hóa Module 09
CREATE INDEX IF NOT EXISTS idx_audit_logs_actor ON audit_logs(actor_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id, created_at DESC);
