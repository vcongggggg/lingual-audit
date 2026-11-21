-- ==============================================================================
-- Module: Gamification Badges & User Badges
-- PostgreSQL 13+
-- Quản lý danh mục huy hiệu vinh danh và lịch sử mở khóa của người dùng
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1. BẢNG DANH MỤC HUY HIỆU (BADGES)
CREATE TABLE IF NOT EXISTS badges (
    id            VARCHAR(32) PRIMARY KEY,
    name          VARCHAR(120) NOT NULL,
    category      VARCHAR(40) NOT NULL,
    rarity        VARCHAR(24) NOT NULL,
    icon          VARCHAR(32) NOT NULL,
    description   VARCHAR(500) NOT NULL,
    max_progress  INT NOT NULL DEFAULT 1,
    xp_reward     INT NOT NULL DEFAULT 0,
    lore          TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. BẢNG TIẾN ĐỘ & MỞ KHÓA HUY HIỆU NGƯỜI DÙNG (USER_BADGES)
CREATE TABLE IF NOT EXISTS user_badges (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    badge_id          VARCHAR(32) NOT NULL REFERENCES badges(id) ON DELETE CASCADE,
    is_unlocked       BOOLEAN NOT NULL DEFAULT false,
    unlocked_at       TIMESTAMPTZ,
    current_progress  INT NOT NULL DEFAULT 0,
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_user_badge UNIQUE (user_id, badge_id)
);

CREATE INDEX IF NOT EXISTS idx_user_badges_user_id ON user_badges(user_id);
CREATE INDEX IF NOT EXISTS idx_user_badges_badge_id ON user_badges(badge_id);

-- 3. SEED DỮ LIỆU BAN ĐẦU CHO 6 HUY HIỆU MẪU SPRINT 1 & 2
INSERT INTO badges (id, name, category, rarity, icon, description, max_progress, xp_reward, lore)
VALUES
    ('b-1', 'Kỷ Lục Gia Streak', 'streak', 'legendary', '🔥', 'Duy trì chuỗi học liên tục trong 14 ngày không đứt quãng.', 14, 500, 'Ngọn lửa kiên trì rực cháy thắp sáng con đường làm chủ ngoại ngữ.'),
    ('b-2', 'Kho Báu Từ Vựng', 'vocabulary', 'epic', '📚', 'Thành thạo vượt mốc 200 từ vựng A1-A2 theo thuật toán SRS SM-2.', 200, 350, 'Một trí tuệ sắc bén lưu giữ hàng trăm từ ngữ sống động.'),
    ('b-3', 'Bất Khả Chiến Bại', 'speed', 'rare', '⚡', 'Đạt tỷ lệ ghi nhớ chính xác 100% trong 5 bài kiểm tra liên tiếp.', 5, 250, 'Tốc độ và sự chuẩn xác kết hợp tạo nên phản xạ phi thường.'),
    ('b-4', 'Chiến Hữu Mezon', 'social', 'common', '🛡️', 'Gia nhập Clan và cùng đồng đội đạt 5,000 XP tuần.', 5000, 150, 'Sức mạnh tập thể nâng bước từng cá nhân vươn lên đỉnh vinh quang.'),
    ('b-5', 'Đại Sứ Học Thuật', 'vocabulary', 'epic', '🎯', 'Hoàn thành toàn bộ gói từ vựng 500 từ cốt lõi chuẩn Bách Khoa.', 500, 750, 'Mục tiêu tối thượng của bất kỳ học giả ngôn ngữ nào trên Mezon.'),
    ('b-6', 'Đấu Vương Thời Gian Thực', 'speed', 'legendary', '👑', 'Chiến thắng 20 trận đối kháng 1vs1 không thua.', 20, 1000, 'Huyền thoại đấu trường trí tuệ được tôn kính trong toàn server Mezon.')
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    category = EXCLUDED.category,
    rarity = EXCLUDED.rarity,
    icon = EXCLUDED.icon,
    description = EXCLUDED.description,
    max_progress = EXCLUDED.max_progress,
    xp_reward = EXCLUDED.xp_reward,
    lore = EXCLUDED.lore;
