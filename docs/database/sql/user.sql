-- Module 01: Identity, Learner Profile & Placement Test (Tinh gọn theo định hướng Mentor Mai Hồng Mận)
-- PostgreSQL 13+. Mezon IDs are external identifiers; never store OAuth tokens here.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Hàm tự động cập nhật timestamp updated_at khi có UPDATE
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

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

-- Chỉ mục tối ưu hóa
CREATE INDEX IF NOT EXISTS idx_users_mezon_id ON users(mezon_user_id);
CREATE INDEX IF NOT EXISTS idx_placement_tests_user ON placement_tests(user_id, started_at DESC);

-- Triggers tự động cập nhật updated_at
DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_learner_profiles_updated_at ON learner_profiles;
CREATE TRIGGER trg_learner_profiles_updated_at
    BEFORE UPDATE ON learner_profiles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
