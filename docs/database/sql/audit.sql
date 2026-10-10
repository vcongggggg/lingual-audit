-- ==============================================================================
-- Module 09: Audit & Security Logging (Nhật ký thao tác nhạy cảm append-only)
-- Theo đặc tả ADM-09, ADM-12 trong docs/experience/role/admin.md và role-permission.md
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1. BẢNG AUDIT_LOGS (Lưu vết thay đổi role, moderation, cấu hình Clan)
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

-- Chỉ mục tối ưu hóa tra cứu nhật ký kiểm toán
CREATE INDEX IF NOT EXISTS idx_audit_logs_actor ON audit_logs(actor_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id, created_at DESC);
