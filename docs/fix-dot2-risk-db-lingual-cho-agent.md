# SPEC FIX ĐỢT 2 — RỦI RO & BUG TIỀM ẨN TRONG SCHEMA LINGUAL (cho coding agent)

Repo: `vcongggggg/lingual-audit`. Baseline: commit `36afeca` (sau khi đã restore `description`).
Nguyên tắc: mọi thay đổi schema phải làm ở **cả 2 nơi** — file module (`docs/database/sql/*.sql`)
**và** `docs/database/sql/00_init_all.sql` (file docker-compose chạy). DBML chỉ cập nhật khi thêm/sửa bảng/cột.

---

## BUG-A [🔴 Cao] Lost update khi nhiều thành viên submit quiz Clan cùng lúc

**Vấn đề:** `clan_quiz_sessions.responses` là 1 mảng JSONB gộp câu trả lời của mọi thành viên.
Nếu backend đọc row → append ở C# → UPDATE (read-modify-write), 2 request đồng thời sẽ ghi đè
lẫn nhau → **mất câu trả lời**, tính sai `winning_user_id`. PRD yêu cầu chịu 500 concurrent users
giờ cao điểm — đây là hot path.

**Fix (không đổi schema — ràng buộc implement):** mọi append vào JSONB mảng phải là **1 câu UPDATE
nguyên tử**, dùng toán tử `||` của Postgres (các UPDATE cùng row tự serialize bằng row lock):
```sql
-- Submit 1 câu trả lời clan quiz (nguyên tử, không lost update)
UPDATE clan_quiz_sessions
SET responses = responses || @response::jsonb
WHERE id = @sessionId
  AND status = 'open'
  AND closes_at > now();
-- Kiểm tra số dòng affected = 1, nếu = 0 nghĩa là phiên đã đóng/không tồn tại → báo lỗi, KHÔNG retry mù.
```
Áp dụng cùng pattern cho các mảng JSONB có nhiều writer:
```sql
-- AI chat: append message + cộng token trong 1 câu
UPDATE ai_conversations
SET messages = messages || @msg::jsonb,
    total_tokens = total_tokens + @tokens
WHERE id = @conversationId;

-- Word duel: mỗi bên append bài làm của mình
UPDATE duel_matches
SET challenger_answers = challenger_answers || @answer::jsonb   -- hoặc opponent_answers
WHERE id = @matchId AND status = 'in_progress';
```
**Cấm:** đọc `responses`/`messages` về C#, append trong code, rồi UPDATE nguyên mảng.

**Ghi vào docs:** thêm mục này vào `docs/database/sql/README.md` (xem BUG-D).

**Kiểm chứng:** mở 2 psql session, chạy 2 UPDATE append cùng `id` gần như đồng thời →
cả 2 thành công, mảng cuối có đủ 2 phần tử (không mất phần tử nào).

---

## BUG-B [🟡 Trung bình] `vocabulary_ids` / `question_ids` JSONB không có FK → orphan

**Vấn đề:** `lessons.vocabulary_ids` → `vocabulary_items(id)` và `duel_matches.question_ids` →
`quiz_questions(id)` chỉ là mảng UUID trong JSONB, DB không kiểm tra. Ghi UUID ma vào →
đọc ra mới phát hiện.

**Fix (thêm trigger validate ở DB — fail closed):** thêm vào sau function `set_updated_at()`
trong `00_init_all.sql` (~dòng 10), `curriculum.sql` và `competition.sql`:
```sql
CREATE OR REPLACE FUNCTION check_jsonb_uuid_refs()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_ARGV[0] = 'lessons' THEN
        IF EXISTS (
            SELECT 1 FROM jsonb_array_elements_text(NEW.vocabulary_ids) AS vid
            WHERE NOT EXISTS (SELECT 1 FROM vocabulary_items v WHERE v.id = vid::uuid)
        ) THEN
            RAISE EXCEPTION 'lessons.vocabulary_ids contains unknown vocabulary_items id';
        END IF;
    ELSIF TG_ARGV[0] = 'duel_matches' THEN
        IF EXISTS (
            SELECT 1 FROM jsonb_array_elements_text(NEW.question_ids) AS qid
            WHERE NOT EXISTS (SELECT 1 FROM quiz_questions q WHERE q.id = qid::uuid)
        ) THEN
            RAISE EXCEPTION 'duel_matches.question_ids contains unknown quiz_questions id';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_lessons_vocab_ref_check ON lessons;
CREATE TRIGGER trg_lessons_vocab_ref_check
    BEFORE INSERT OR UPDATE OF vocabulary_ids ON lessons
    FOR EACH ROW EXECUTE FUNCTION check_jsonb_uuid_refs('lessons');

DROP TRIGGER IF EXISTS trg_duel_question_ref_check ON duel_matches;
CREATE TRIGGER trg_duel_question_ref_check
    BEFORE INSERT OR UPDATE OF question_ids ON duel_matches
    FOR EACH ROW EXECUTE FUNCTION check_jsonb_uuid_refs('duel_matches');
```
Lưu ý: đặt trigger trong `curriculum.sql` (cho lessons) và `competition.sql` (cho duel_matches),
đồng thời copy vào `00_init_all.sql` đúng vị trí module. Function dùng chung đặt 1 lần ở đầu file.

**Quy ước bổ sung (ghi vào README):** không bao giờ DELETE cứng `vocabulary_items` /
`quiz_questions` đang được tham chiếu — dùng `status='archived'`.

**Kiểm chứng:**
```sql
INSERT INTO lessons (unit_id, code, title, vocabulary_ids)
VALUES ('<unit_id_thật>', 't1', 't', '["00000000-0000-0000-0000-000000000000"]');
-- phải báo lỗi 'contains unknown vocabulary_items id'
```

---

## BUG-C [🔴 Cao] `placement_tests`: 1 user mở được nhiều bài placement cùng lúc

**Vấn đề:** không có gì ngăn user tạo 2 `placement_tests` `in_progress` song song →
`assessed_level` ghi đè, `learner_profiles.proficiency_level` không biết lấy từ bài nào.
(`quiz_attempts` đã có unique tương tự ở FIX-9 — đây là chỗ sót.)

**Fix:** thêm partial unique index. Vị trí: `docs/database/sql/user.sql` dòng ~64
(cạnh `idx_placement_tests_user`) và `docs/database/sql/00_init_all.sql` dòng ~75:
```sql
CREATE UNIQUE INDEX IF NOT EXISTS uq_placement_test_in_progress
    ON placement_tests(user_id) WHERE status = 'in_progress';
```

**Kiểm chứng:**
```sql
INSERT INTO placement_tests (user_id) VALUES ('<uuid>');  -- lần 1 ok
INSERT INTO placement_tests (user_id) VALUES ('<uuid>');  -- lần 2 phải lỗi unique
UPDATE placement_tests SET status='completed' WHERE ...;  -- xong thì tạo mới lại được
```

---

## BUG-D [🟡 Trung bình] Race khi tạo dòng đầu tiên (upsert bắt buộc)

**Vấn đề:** `user_streaks` (PK `user_id`), `lesson_progress` (PK `user_id, lesson_id`),
`srs_cards` (PK `user_id, vocabulary_id`) đều insert-on-first-use. 2 request đầu tiên đồng thời
→ cùng `INSERT` → 1 bên dính unique violation nếu backend check-then-insert.

**Fix (không đổi schema — ràng buộc implement):** backend **bắt buộc** dùng `INSERT ... ON CONFLICT`,
không được SELECT-then-INSERT:
```sql
-- Mẫu cho user_streaks (ngày đầu user hoạt động)
INSERT INTO user_streaks (user_id, current_days, best_days, last_active_date)
VALUES (@userId, 1, 1, CURRENT_DATE)
ON CONFLICT (user_id) DO NOTHING;

-- Mẫu cho lesson_progress / srs_cards
INSERT INTO lesson_progress (user_id, lesson_id, status)
VALUES (@userId, @lessonId, 'in_progress')
ON CONFLICT (user_id, lesson_id) DO NOTHING;
```

**Kiểm chứng:** review code backend (khi viết xong) — grep mọi chỗ insert 3 bảng này phải có
`ON CONFLICT`.

---

## BUG-E [🟡 Trung bình] `freeze_balance` có thể bị trừ 2 lần 1 vé

**Vấn đề:** nếu backend đọc balance → trừ 1 ở C# → UPDATE, 2 request cùng qua ngưỡng ngày
sẽ double-spend vé freeze streak.

**Fix (không đổi schema — ràng buộc implement):** trừ nguyên tử + kiểm tra affected rows:
```sql
UPDATE user_streaks
SET freeze_balance = freeze_balance - 1,
    freeze_history = freeze_history || jsonb_build_object('used_at', now(), 'reason', @reason)
WHERE user_id = @userId AND freeze_balance > 0;
-- affected = 1 → trừ thành công; affected = 0 → hết vé → báo lỗi, KHÔNG cho qua
```

**Kiểm chứng:** khi có backend, test 2 request đồng thời với `freeze_balance = 1` →
tổng số lần trừ thành công trên DB phải đúng 1.

---

## BUG-F [🟢 Thấp] Thiếu CHECK thời gian hợp lệ

**Vấn đề:** `clan_quiz_sessions` không có ràng buộc `closes_at > opened_at` → bug backend có thể
tạo phiên "chưa mở đã đóng". Tương tự `completed_at >= started_at` ở `placement_tests`, `duel_matches`.

**Fix:** thêm CHECK inline trong `CREATE TABLE` ở cả file module và `00_init_all.sql`:
- `community.sql` (~dòng 45) + `00_init_all.sql` (~dòng 366), trong block `clan_quiz_sessions`,
  thêm dòng: `CHECK (closes_at > opened_at),`
- `user.sql` (~dòng 48) + `00_init_all.sql` (~dòng 59), trong block `placement_tests`,
  thêm dòng: `CHECK (completed_at IS NULL OR completed_at >= started_at),`
- `competition.sql` (~dòng 11) + `00_init_all.sql` (~dòng 414), trong block `duel_matches`,
  thêm dòng: `CHECK (completed_at IS NULL OR completed_at >= started_at),`

**Kiểm chứng:**
```sql
INSERT INTO clan_quiz_sessions (channel_mezon_id, question_id, closes_at, opened_at)
VALUES ('c', '<qid>', now(), now() + interval '1 hour');  -- phải lỗi CHECK
```

---

## Ghi chú hardening (tùy chọn, làm sau)

- `REVOKE UPDATE, DELETE ON audit_logs FROM <app_role>;` — hiện mới chỉ ghi trong README.
  Chỉ đưa vào `audit.sql` khi đã tách role app riêng khỏi superuser (hiện docker-compose dùng
  1 role `lingual_user` là superuser nên REVOKE chưa có tác dụng thực tế).

---

## Kiểm chứng tổng sau khi làm hết

1. `docker compose up -d` trên DB trống → init thành công, `\dt` ra **22 bảng**.
2. Chạy `00_init_all.sql` lần 2 → không lỗi (idempotency: `IF NOT EXISTS` + `DROP TRIGGER IF EXISTS`).
3. Diff body từng bảng giữa 8 file module và `00_init_all.sql` → khớp 100% (script check đã dùng ở đợt 1).
4. Chạy từng câu "phải lỗi" trong các mục BUG-B, BUG-C, BUG-F → đều bị từ chối đúng như kỳ vọng.
5. `git diff --stat` review: chỉ chạm các file đã liệt kê, không rớt cột nào (bài học từ vụ `description`).
