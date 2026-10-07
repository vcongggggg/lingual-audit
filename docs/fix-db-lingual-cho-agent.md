# SPEC FIX DATABASE — LINGUAL (cho coding agent)

Repo: `vcongggggg/lingual-audit` @ commit `8cb7931`. File init chính: `docs/database/sql/00_init_all.sql`
(là file `docker-compose.yml` mount vào `/docker-entrypoint-initdb.d/` — mọi fix phải áp dụng ở đây).

Quy tắc: sửa xong mỗi mục, chạy phần **Kiểm chứng** tương ứng. Không tin "nhìn là đúng".

---

## FIX-1 [P0] Thiếu `CREATE EXTENSION pgcrypto` → init DB fail trên database mới

**Vấn đề:** Mọi PK UUID dùng `DEFAULT gen_random_uuid()` — hàm của extension `pgcrypto`, không có sẵn
trong database mới của image `postgres:16-alpine`. Không file nào trong repo tạo extension này
(đã grep toàn repo: 0 kết quả). PostgreSQL validate default expression ngay tại `CREATE TABLE`
→ lỗi `function gen_random_uuid() does not exist` ở bảng `users`, toàn bộ init đổ.

**Fix:** thêm dòng sau ngay đầu file, sau block header comment (dòng 1–6), trước `MODULE 01`:
- `docs/database/sql/00_init_all.sql` (dòng ~7)
- Và đầu mỗi file module để chạy lẻ được: `user.sql`, `curriculum.sql`, `learning.sql`,
  `quiz.sql`, `community.sql`, `competition.sql`, `assistant.sql`
```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

**Kiểm chứng:** `docker compose up -d` → container postgres healthy, bảng `users` được tạo,
`SELECT * FROM pg_extension WHERE extname='pgcrypto';` trả về 1 dòng.

---

## FIX-2 [P0] `duel_matches` định nghĩa 2 lần trong `00_init_all.sql` → DB thật thiếu 7 cột

**Vấn đề:** `docs/database/sql/00_init_all.sql` có 2 `CREATE TABLE IF NOT EXISTS duel_matches`:
- Dòng **321**: bản tối giản tiếng Anh (chỉ: id, challenger_id, opponent_id, status,
  question_count, winner_id, started_at, completed_at, created_at) — **thiếu**
  `question_ids, challenger_answers, opponent_answers, challenger_score, opponent_score,
  challenger_correct, opponent_correct`.
- Dòng **333**: bản đầy đủ tiếng Việt (đúng).
- Do `IF NOT EXISTS`, bản đầy đủ bị bỏ qua lặng lẽ → DB do docker-compose tạo ra thiếu 7 cột.

**Fix:** xóa **toàn bộ** block từ dòng 318 đến 331 (giữ lại bản đầy đủ từ dòng 332 trở đi).
Block cần xóa chính xác:
```sql
-- ==========================================================================
-- Module 06: asynchronous duel sessions and finalized weekly standings.
CREATE TABLE IF NOT EXISTS duel_matches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    challenger_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    opponent_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status VARCHAR(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','accepted','in_progress','completed','declined','expired','cancelled')),
    question_count SMALLINT NOT NULL DEFAULT 5 CHECK (question_count > 0),
    winner_id UUID REFERENCES users(id) ON DELETE SET NULL,
    started_at TIMESTAMPTZ, completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(), CHECK (challenger_id <> opponent_id)
);

```
(Không xóa dòng comment `-- 1. BẢNG TRẬN ĐẤU ĐỐI KHÁNG...` và block CREATE ở dòng 333.)

**Kiểm chứng:**
- `grep -c "CREATE TABLE IF NOT EXISTS duel_matches" docs/database/sql/00_init_all.sql` → đúng **1**.
- Sau init: `\d duel_matches` có 18 cột, gồm `question_ids`, `challenger_answers`, `opponent_answers`,
  `challenger_score`, `opponent_score`, `challenger_correct`, `opponent_correct`.
- Diff body bảng `duel_matches` giữa `00_init_all.sql` và `competition.sql` → giống nhau.

---

## FIX-3 [P1] Header ghi sai "45 Tables" → sửa thành 20

**Vấn đề:** `docs/database/sql/00_init_all.sql` dòng 3 ghi `Combined 8 Modules · 45 Tables`
— thực tế 20 bảng (module analytics.sql chỉ là comment, không có bảng).
`scripts/init_database.py` cũng in `8 modules · 45 bảng`.

**Fix:**
- `00_init_all.sql` dòng 3 → `-- Combined 8 Modules · 20 Tables · Single-Clan Mezon Architecture`
  (hoặc `7 Modules · 20 Tables` cho khớp DBML — chọn 1 và đồng nhất).
- `scripts/init_database.py`: sửa chuỗi `45 bảng` → `20 bảng`.

**Kiểm chứng:** `grep -rn "45" docs/database/sql/00_init_all.sql scripts/init_database.py` → 0 kết quả.

---

## FIX-4 [P1] Thêm bảng `audit_logs` (tài liệu bắt buộc, schema đang thiếu)

**Vấn đề:** `docs/experience/role/admin.md` (ADM-09, ADM-12) và `role-permission.md` yêu cầu ghi audit
cho mọi thay đổi role, moderation, cấu hình, thao tác nhạy cảm; "người dùng ứng dụng không được
sửa/xóa audit record". Schema hiện **không có bảng audit nào**.

**Fix:** tạo file mới `docs/database/sql/audit.sql`, append nội dung tương tự vào cuối
`00_init_all.sql` (sau MODULE 08, đánh số MODULE 09), cập nhật `docs/database/sql/README.md`
(thứ tự chạy) và `docs/database/lingual_full_schema.dbml`:
```sql
-- MODULE 09: AUDIT.SQL — Nhật ký thao tác nhạy cảm (append-only)
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
CREATE INDEX IF NOT EXISTS idx_audit_logs_actor ON audit_logs(actor_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id, created_at DESC);
```
Lưu ý: `audit_logs` phải đặt SAU `users` (có FK tới users). Sau khi tạo, chạy thêm:
```sql
REVOKE UPDATE, DELETE ON audit_logs FROM lingual_user;
```
(nếu app dùng role `lingual_user` — điều chỉnh theo role thực tế; ghi lại trong README).

**Kiểm chứng:** init xong, `\d audit_logs` tồn tại; thử `UPDATE audit_logs ...` bằng app role → bị từ chối.

---

## FIX-5 [P1] Thêm bảng `clan_moderator_grants` (tài liệu bắt buộc, schema đang thiếu)

**Vấn đề:** `docs/experience/role/clan-moderator.md` quy định grant `clan_moderator` "được lưu tách biệt
với role toàn cục", có cấp/thu hồi/hiệu lực, do Admin cấp. `users.role` CHECK chỉ cho phép
`('learner','moderator','admin')` — không nơi nào lưu grant này.

**Fix:** thêm vào `docs/database/sql/community.sql` (sau `clan_quiz_sessions`), vào `00_init_all.sql`
(MODULE 05), và DBML:
```sql
-- Bảng grant Clan Moderator (quyền vận hành phạm vi 1 Clan, tách biệt role toàn cục)
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
CREATE INDEX IF NOT EXISTS idx_clan_mod_grants_user ON clan_moderator_grants(user_id) WHERE revoked_at IS NULL;
```

**Kiểm chứng:** init xong `\d clan_moderator_grants` tồn tại; insert grant rồi query
`WHERE revoked_at IS NULL AND (expires_at IS NULL OR expires_at > now())` → ra đúng user.

---

## FIX-6 [P1] `updated_at` không tự cập nhật → thêm trigger

**Vấn đề:** 11 bảng có `updated_at TIMESTAMPTZ NOT NULL DEFAULT now()` nhưng không có trigger
→ UPDATE bằng SQL không bao giờ đổi `updated_at`. Các bảng: `users`, `learner_profiles`,
`courses`, `lessons`, `vocabulary_items`, `srs_cards`, `user_streaks`, `quizzes`,
`quiz_questions`, `bot_configuration`, `ai_scenarios`.

**Fix:** thêm vào đầu `00_init_all.sql` (sau `CREATE EXTENSION`, cần FIX-1 trước) và mỗi file
module tương ứng:
```sql
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```
Rồi với mỗi bảng trong danh sách trên:
```sql
DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
```
(đặt tên trigger theo pattern `trg_<bảng>_updated_at`).

**Kiểm chứng:** `UPDATE users SET display_name='x' WHERE ...;` → `updated_at` đổi theo;
`SELECT count(*) FROM pg_trigger WHERE tgname LIKE 'trg\_%\_updated_at';` → 11.

---

## FIX-7 [P2] Thêm index cho FK đang thiếu

**Vấn đề:** các FK sau không có index → DELETE/UPDATE phía bảng cha và JOIN bị seq-scan:
```sql
CREATE INDEX IF NOT EXISTS idx_clan_quiz_question ON clan_quiz_sessions(question_id);
CREATE INDEX IF NOT EXISTS idx_ai_conv_scenario ON ai_conversations(scenario_id);
CREATE INDEX IF NOT EXISTS idx_duel_winner ON duel_matches(winner_id);
CREATE INDEX IF NOT EXISTS idx_weekly_lb_xp ON weekly_leaderboards(week_start, xp_total DESC);
```
Thêm vào file module tương ứng (`community.sql`, `assistant.sql`, `competition.sql`) và
`00_init_all.sql` đúng vị trí module.

**Kiểm chứng:** `\di` liệt kê đủ 4 index sau init.

---

## FIX-8 [P2] Chống cộng XP trùng khi app quên sinh idempotency_key

**Vấn đề:** `xp_ledger` chỉ chống trùng bằng `idempotency_key` do app sinh; nếu app quên sinh key,
cùng `(source_type, source_id)` vẫn insert trùng được.

**Fix:** thêm vào `learning.sql` và `00_init_all.sql` (MODULE 03):
```sql
CREATE UNIQUE INDEX IF NOT EXISTS uq_xp_ledger_source
    ON xp_ledger(source_type, source_id) WHERE source_id IS NOT NULL;
```

**Kiểm chứng:** insert 2 dòng cùng `(source_type, source_id)` không có idempotency_key → dòng 2 bị từ chối.

---

## FIX-9 [P2] Các fix nhỏ nhất quán

1. `quizzes.code`: `VARCHAR(80) UNIQUE` đang nullable (PG cho phép nhiều NULL) — không nhất quán với
   `courses.code NOT NULL UNIQUE`. **Fix:** `ALTER TABLE quizzes ALTER COLUMN code SET NOT NULL;`
   (hoặc giữ nullable nhưng ghi rõ lý do trong comment).
2. `quiz_attempts`: chống 2 attempt `in_progress` cùng lúc:
   ```sql
   CREATE UNIQUE INDEX IF NOT EXISTS uq_quiz_attempt_in_progress
       ON quiz_attempts(quiz_id, user_id) WHERE status = 'in_progress';
   ```
3. Xóa file trùng: `database.zip` ở root giống hệt `docs/database.zip` (cùng 20875 bytes) → xóa bản ở root.
4. 8 file `docs/reference/*.md` đang trống 0 dòng (api, events, notifications, audit-logs, risk-rules,
   data-model, mezon-channel-app, mezon-notifications-inventory) → hoặc điền nội dung, hoặc xóa file.
   Riêng `audit-logs.md` nên điền sau khi làm FIX-4.
5. Cập nhật `docs/database/lingual_full_schema.dbml`: thêm `audit_logs`, `clan_moderator_grants`,
   và các index mới để ERD khớp SQL.

**Kiểm chứng tổng (sau tất cả fix):**
- `grep -c "^CREATE TABLE" docs/database/sql/00_init_all.sql` → **22** (20 + 2 bảng mới; trừ 1 duplicate đã xóa: 21 dòng CREATE trong file cũ − 1 + 2 = 22).
- Chạy `00_init_all.sql` **2 lần liên tiếp** trên DB trống → không lỗi (idempotency).
- Diff danh sách bảng/cột giữa `00_init_all.sql` và 8 file module → khớp 100%.
