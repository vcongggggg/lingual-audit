-- ==============================================================================
-- Module 02: Curriculum & Vocabulary (Tinh gọn từ 7 bảng còn 4 bảng theo Phương án A)
-- PostgreSQL 13+. Tối ưu hóa:
--  1. Gộp bảng vocabulary_examples vào cột examples (JSONB) trong vocabulary_items.
--  2. Bỏ bảng trung gian lesson_vocabulary: Gộp danh sách ID từ vựng vào cột vocabulary_ids (JSONB) trong lessons.
--  3. Bỏ bảng user_vocabulary_deck: Tính năng lưu/ôn từ vựng đã được quản lý tập trung bởi srs_cards trong Module Learning.
-- ==============================================================================

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
    -- Danh sách ID các từ vựng thuộc bài học này:
    -- Ví dụ: ["b3e0c034-7a1f-4f76-a4c8-b1c4e5f76b89", "7c29e120-d306-4b82-9387-5c2f0f8a8451"]
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
    -- Ví dụ câu ngữ cảnh gộp vào JSONB:
    -- Ví dụ: [{"source_text": "I eat an apple", "target_text": "Tôi ăn một quả táo", "audio_url": "https://..."}]
    examples                JSONB NOT NULL DEFAULT '[]'::jsonb,
    status                  VARCHAR(16) NOT NULL DEFAULT 'published' CHECK (status IN ('draft', 'published', 'archived')),
    created_by              UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(normalized_term, source_language, target_language)
);

-- Chỉ mục tối ưu hóa tìm kiếm và truy vấn
CREATE INDEX IF NOT EXISTS idx_units_course ON units(course_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_lessons_unit ON lessons(unit_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_vocabulary_search ON vocabulary_items(normalized_term, status);
CREATE INDEX IF NOT EXISTS idx_vocabulary_lang ON vocabulary_items(source_language, target_language);
