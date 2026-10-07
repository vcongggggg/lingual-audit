# LINGUAL — GÓI CHUẨN BỊ MILESTONE 1 (12/10/2026)

> Team 05 · Mezon Campus Studio 2026 · Soạn ngày 04/10/2026
> Gồm: (0) Điểm lệch cần chốt · (1) User Stories & AC · (2) Đặc tả CSDL · (3) Kế hoạch Sprint 0 từng ngày + DoD
> File đi kèm: `lingual_full_schema.dbml` (dán vào dbdiagram.io để có ERD 45 bảng 8 module ngay)

---

## 0. ĐIỂM LỆCH GIỮA CÁC TÀI LIỆU — CẦN CHỐT TRƯỚC KHI NỘP

Khi đối chiếu PRD, Sprint Plan, ARCHITECTURE_DESIGN.md và schema.prisma, mình thấy các điểm sau. Mỗi điểm có **quyết định đề xuất** (đã dùng làm cơ sở cho phần 1–3). Công xem nhanh 30 phút là chốt được.

| # | Điểm lệch | Đề xuất |
|---|-----------|---------|
| D1 | **Nút đánh giá SRS**: PRD là 4 nút (Again/Hard/Good/Easy), Sprint Plan SP1-06 là 5 nút (1–5) | Dùng **4 nút** (chuẩn UX Anki/Duolingo). Map sang quality SM-2: Again=1, Hard=3, Good=4, Easy=5. DB vẫn lưu quality 0–5 để unit test đủ kịch bản |
| D2 | **Thời gian /quiz**: PRD 30 giây, Sprint Plan SP2-03 là 15 giây | **30 giây** (PRD là nguồn business rule), để thành config `ClanQuiz:DurationSeconds`. Sửa SP2-03 |
| D3 | **Quy tắc Streak**: FR-07.3 nói "5 phút hoặc 1 quiz", mục 7.2 nói "≥ 20 XP/ngày" | Chỉ giữ **≥ 20 XP/ngày** (múi giờ Asia/Ho_Chi_Minh). Sửa FR-07.3 |
| D4 | **Luồng UX Word Duel trên Mezon**: Chơi trên chat bot hay Webview | **Chuẩn Mezon UX 3 bước**: Thách đấu trên Chat Bot (`/duel @user`) ➔ Chấp nhận thì mở **Channel Mini-App (Webview nhúng Mezon)** đấu Realtime qua SignalR (không spam chat, đếm ngược 10s mượt mà, có âm thanh) ➔ Kết thúc trận Bot tự động bắn **Thẻ vinh danh kết quả (+XP)** ra kênh Chat Clan |
| D5 | **Ma trận phạm vi PRD (mục 8)** xếp Interactive Buttons, đố vui realtime và Word Duel vào "Phase 2 (tuần 5–8)", còn Sprint Plan đã xếp /quiz vào Sprint 2 và Duel vào Sprint 3 | Sửa nhãn Phase trong PRD cho khớp Sprint Plan để Mentor không hỏi. ERD vẫn gồm bảng Duel và /quiz vì Milestone 1 chốt thiết kế, code theo Sprint |
| D6 | **ARCHITECTURE_DESIGN.md mô tả stack khác thực tế**: FastAPI/Express, Prisma/SQLAlchemy, "Node.js or Python", không có SignalR, không có module map | Viết lại theo .NET 8 Modular Monolith (7 project), EF Core, SignalR GameHub, Adapter Mezon. Đã có khung ở Task Công ngày 06/10 |
| D7 | **Đặc tả yếu tố AI cho đề tài**: Đề tài đăng ký là *"Web app học TA tích hợp AI"* nhưng bản thảo cũ chưa có bảng AI | Bổ sung module **AI Tutor (LingLing)** (`ai_scenarios`, `ai_conversations`, `ai_messages`) lưu vết phân tích ngữ pháp, kịch bản hội thoại và token Google Gemini API, đảm bảo 100% tiêu chí đề tài |
| D8 | **Kiến trúc Mezon Bot Single-Clan vs Multi-Clan**: Quản lý đa Clan phức tạp hay tập trung vào 1 Clan | **Chốt Single-Clan Architecture**: Bot được thiết kế chuyên biệt phục vụ cho **1 Clan cụ thể** (trường ĐH hoặc cộng đồng sinh viên Mezon) thông qua bản ghi singleton `bot_configuration`. Không cần bảng `clans` đa cấp phức tạp, thành viên quản lý qua `configured_clan_members` và `configured_clan_role_grants` |

**Về bộ CSDL chính thức của dự án:** Nhóm đã chuẩn hóa CSDL gồm **8 file SQL chuyên nghiệp (45 bảng chuẩn PostgreSQL 13+)** lưu trữ tại [docs/database/sql/](file:///c:/Study/HocKy6/MezonCampusStudio/docs/database/sql). Để giảm tải cho Minh BE và đảm bảo tiến độ nộp Milestone 1 (12/10), nhóm áp dụng chiến lược **Phân kỳ Migration (Phase 1: ~20 bảng Core MVP ở Sprint 1–3; Phase 2: 25 bảng Nâng cao ở Sprint 4–6)**.

---

## 1. USER STORIES & ACCEPTANCE CRITERIA

Quy ước: **P0** = bắt buộc cho demo cuối; **P1** = làm nếu còn thời gian. Owner theo luồng (Công = Bot/AI/SignalR, Minh = DB/BE/SRS, Trí = FE/Data/QA).

### LUỒNG 1 — Học từ vựng cá nhân (Flashcard 3D + SRS SM-2)

**US-101 · Xem lộ trình học** (P0 · Trí/Minh)
Là người học, tôi muốn xem Course → Unit → Lesson theo trình độ của mình để biết học gì tiếp.
- Given tôi đã đăng nhập, When mở Dashboard, Then thấy danh sách Unit của course đúng `cefr_level` của tôi, mỗi Lesson hiển thị trạng thái (chưa học / đang học / hoàn thành).
- Lesson hoàn thành dựa trên `user_lesson_progress`; Lesson chưa publish (`status != published`) không hiển thị.
- API trả danh sách trong < 200 ms với dữ liệu 500 từ.

**US-102 · Học thẻ từ vựng 3D** (P0 · Trí)
Là người học, tôi muốn lật thẻ để xem nghĩa, IPA, ví dụ và nghe phát âm.
- Given đang trong một Lesson, When bấm thẻ hoặc phím Space, Then thẻ lật 3D, mặt sau hiện nghĩa tiếng Việt, loại từ, ví dụ EN/VI.
- Có nút phát audio; nếu `audio_url` null thì ẩn nút, không lỗi.
- Lần đầu thẻ xuất hiện, hệ thống tạo `srs_states` (state=`new`) cho cặp (user, từ) nếu chưa có.

**US-103 · Đánh giá nhớ/quên (SM-2)** (P0 · Minh + Trí)
Là người học, tôi muốn chọn Again/Hard/Good/Easy để hệ thống lên lịch ôn đúng lúc.
- Given đang xem mặt sau thẻ, When bấm một nút, Then API ghi `srs_review_logs`, cập nhật `srs_states` (repetitions, interval_days, ease_factor, next_review_at, lapses) và trả thẻ kế tiếp.
- Quy tắc (SM-2 chuẩn): quality ≥ 3 thì repetitions=0→interval 1 ngày, =1→6 ngày, ≥2→`round(interval × EF)`; quality < 3 thì repetitions=0, interval=1, lapses+1. `EF' = EF + (0.1 − (5−q)(0.08 + (5−q)·0.02))`, tối thiểu **1.30**.
- Again: thẻ quay lại cuối phiên hiện tại. Unit test phủ q = 0..5 và biên EF = 1.30 (≥ 75% coverage theo PRD).
- Gọi API 2 lần liên tiếp cho cùng một thẻ trong 1 giây chỉ ghi nhận 1 lần (chống double-click).

**US-104 · Hàng đợi ôn tập hôm nay** (P0 · Minh/Trí)
Là người học, tôi muốn thấy các từ đến hạn hôm nay.
- Given có từ với `next_review_at <= now()` và `state != 'new'`, When mở "Ôn tập", Then danh sách sắp theo `next_review_at` tăng dần, giới hạn 20 thẻ/phiên (config).
- Không có từ đến hạn thì hiện trạng thái "Hôm nay đã xong".

**US-105 · Hoàn thành bài học, nhận XP** (P0 · Minh)
- When hoàn thành mọi thẻ của Lesson, Then `user_lesson_progress.status=completed`, +25 XP (`lessons.xp_reward`) **đúng một lần**, kèm +5 XP cho mỗi từ mới lần đầu học.
- XP ghi vào `xp_events` với `source_ref` để chống cộng trùng; `users.total_xp` cập nhật cùng giao dịch.
- Đạt ≥ 20 XP trong ngày local thì streak +1 (một lần/ngày). Quên 1 ngày: còn `streak_freezes` thì tự trừ 1, hết thì streak=0.

**US-106 · Lưu từ vào sổ tay** (P1 · Minh/Trí)
- When bấm "Lưu từ" ở bất kỳ thẻ nào, Then tạo `srs_states` (source=`bookmark`) nếu chưa có; bấm lại không tạo trùng.

**US-107 · Thống kê nhanh** (P1 · Trí)
- Dashboard hiện: số từ đến hạn hôm nay, streak, tổng XP, số từ `review` đã học.

### LUỒNG 2 — Mezon Clan Bot & Word Duel realtime

**US-201 · Tự động tạo tài khoản & Mezon SSO Handshake** (P0 · Công/Minh)
Là thành viên Clan, tôi sử dụng Bot hoặc mở Channel Mini-App mà không phải trải qua thủ tục đăng ký rườm rà.
- Given tôi chưa có tài khoản, When gõ `/learn` trên Bot hoặc mở tab Channel Mini-App trên Mezon, Then hệ thống tự động tạo `users` (từ `mezon_user_id`, display name, avatar) và `clan_members` nếu chưa có, trả từ đầu tiên **< 30 giây**.
- **Mezon SSO Handshake:** Khi client Next.js mở trong Webview Mezon, client gửi context token lên `/api/v1/auth/mezon-sso` để xác thực chữ ký số và nhận JWT phiên làm việc.
- Mọi webhook từ Mezon Gateway đều kiểm tra **chữ ký HMAC** (`X-Mezon-Signature`); request sai chữ ký bị từ chối HTTP 401.

**US-202 · `/learn` — 3 từ của hôm nay** (P0 · Công)
- When gõ `/learn`, Then bot gửi 3 từ mới (chưa có trong `srs_states` của tôi, đúng `cefr_level`) gồm từ, IPA, nghĩa, ví dụ.
- 3 từ được tạo `srs_states` (source=`bot_learn`, state=`new`, `next_review_at=now()`) để xuất hiện ở `/review` và trên web.
- Hết từ mới trong trình độ thì bot báo rõ, không gửi trống. Phản hồi < 1 giây (ngưỡng SP1-05; mục tiêu PRD 500 ms).

**US-203 · `/quiz` — đố vui trong kênh** (P0 · Công)
- When gõ `/quiz`, Then bot đăng 1 câu hỏi 4 nút (A–D), mở `clan_quiz_sessions` với `ends_at = now + 30s`.
- Mỗi người chỉ được trả lời **một lần** (UNIQUE `session_id, user_id`); bấm lần hai nhận thông báo "đã trả lời". Nhiều người bấm cùng lúc không gây lỗi hoặc ghi trùng.
- Hết giờ: bot công bố đáp án, giải thích, người đúng nhanh nhất. XP: đúng +10; người đúng đầu tiên và trả lời trong 5 giây đầu +5 thêm.
- Thời gian trả lời tính bằng đồng hồ server (`response_ms`). Bấm sau `ends_at` bị bỏ qua.
- Một kênh chỉ có tối đa 1 session `open` tại một thời điểm.

**US-204 · `/streak` và `/review`** (P1 · Công)
- `/streak` trả streak hiện tại, số Streak Freeze, XP hôm nay (còn thiếu bao nhiêu để đủ 20 XP).
- `/review` trả tối đa 5 từ đến hạn.

**US-205 · Thách đấu Word Duel trên Kênh Chat (`/duel @user`)** (P0 · Công)
- When gõ `/duel @user` trong kênh Clan, Then tạo `matches` (status=`pending`) và bot gửi tin nhắn thách đấu công khai kèm nút **[Chấp nhận]** / **[Từ chối]**.
- Không thể tự thách đấu chính mình (CHECK DB). Người được mời đang có trận khác thì bot báo bận.
- Không phản hồi trong 60 giây thì `status=expired`.

**US-206 · Đấu trường 5 vòng Realtime trên Channel Mini-App (SignalR)** (P0 · Công + Trí)
- When người được mời bấm [Chấp nhận], Bot cung cấp nút **[VÀO ĐẤU TRƯỜNG NGAY]**. Cả 2 mở màn hình **Webview nhúng trực tiếp trong Mezon**, tự động kết nối SignalR GameHub.
- Trận đấu diễn ra độc lập trong Webview để **hoàn toàn không làm rác/spam kênh chat Clan**, đồng thời đảm bảo đếm ngược 10s mượt mà, có âm thanh và hiệu ứng sinh động.
- Cả hai nhận **cùng lúc** câu hỏi từng vòng (10 giây/vòng); server là nguồn sự thật (không gửi `correct_index` xuống client trước khi vòng kết thúc).
- Mỗi người trả lời một lần mỗi vòng (UNIQUE `match_round_id, user_id`); không trả lời = `selected_index null`, 0 điểm.
- Điểm vòng: đúng = 100 + `round(100 × (T − t)/T)` với T = 10 000 ms; sai/hết giờ = 0. Điểm đối thủ cập nhật realtime qua SignalR.

**US-207 · Kết thúc trận, vinh danh ra Kênh Chat Clan & XP** (P0 · Minh + Công)
- When đủ 5 vòng, Then `matches.status=finished`, lưu `winner_id` (null nếu hòa), điểm hai bên, `finished_at`.
- XP: thắng +30, thua +10 (PRD 7.1); hòa +20 mỗi người. XP ghi vào `xp_events` với `source_ref = match_id` (chỉ cộng một lần).
- **Vinh danh Clan:** Bot tự động bắn 1 Thẻ Card kết quả đẹp mắt vào lại kênh chat Clan: *"🏆 @Công vừa thắng @Minh với điểm số 750 - 580 (4/5 câu đúng) trong trận Word Duel! (+30 XP cho Clan)"*.
- Bảng xếp hạng Clan tuần cập nhật ngay trên Redis.

**US-208 · Rớt mạng giữa trận** (P0 · Công/Trí)
- Mất kết nối < 10 giây: cho Reconnect vào đúng vòng hiện tại (trạng thái giữ trên Redis). Quá 10 giây: `end_reason=forfeit_disconnect`, đối thủ thắng, người rớt không nhận XP.
- Client còn lại không bị treo; server giải phóng room.

**US-209 · Chống spam** (P0 · Công/Minh)
- Quá 5 request/giây/user (sliding window Redis) thì trả 429 / bot im lặng bỏ qua, không crash.

### LUỒNG 3 — Trợ lý AI Đồng hành LingLing (Gemini API)

**US-301 · Sửa lỗi ngữ pháp (P0) & Chat tình huống thời gian thực (P1)** (Công)
Là người học, tôi muốn gửi câu tiếng Anh bất kỳ để AI phân tích lỗi và luyện giao tiếp phản xạ.
- Given tôi mở tab LingLing AI trên Mini-App hoặc gõ `/ask [câu tiếng Anh]` trên Bot.
- Backend điều phối qua `Lingual.Modules.AITutor` gọi Google Gemini API:
  1. Khen ngợi điểm ngữ pháp đúng.
  2. Chỉ rõ lỗi sai (nếu có) và nguyên nhân.
  3. Gợi ý 1–2 cách diễn đạt chuẩn bản xứ (Native expression) kèm dịch nghĩa tiếng Việt.
- Ghi nhận vào bảng `ai_chat_logs` (gồm user_id, prompt, ai_response, feedback_json) để người học có thể xem lại "Sổ tay lỗi ngữ pháp đã được AI sửa".
- **Phân kỳ rõ ràng:** 
  - Tính năng **Sửa lỗi ngữ pháp (Grammar Check)** là **P0** (chạy được demo ngay ở Sprint 1 phục vụ tiêu chí đề tài AI).
  - Tính năng **Đóng vai tình huống (Roleplay Barista/Phỏng vấn)** là **P1** (triển khai nâng cao ở Sprint 4).

### 1.1 Lưu ý kỹ thuật Mezon SSO & Webview (Spike Sprint 1 cho Công)
- **Token Storage trong Webview:** Webview chạy trong ngữ cảnh cross-site với Mezon, tuyệt đối không phụ thuộc vào `httpOnly` Cookie. Client Next.js lưu JWT trong bộ nhớ (hoặc `sessionStorage`) và gửi qua `Authorization: Bearer <token>`.
- **SignalR Token Authentication:** Trình duyệt không thể gắn header tùy biến cho kết nối WebSocket. Do đó, client SignalR gửi JWT qua query string `?access_token=<token>`, và Backend .NET 8 phải cấu hình `JwtBearerEvents.OnMessageReceived` để trích xuất token từ Query String.
- **CSP Frame-Ancestors:** Header Content-Security-Policy của Next.js và API phải cho phép `frame-ancestors 'self' https://*.mezon.vn mezon:;` để tránh bị chặn khi nhúng iframe trong ứng dụng Mezon Desktop/Web.
- **Sprint 1 Spike:** Lên lịch spike 1 ngày ở đầu Sprint 1 để kiểm chứng thực tế cơ chế truyền token và nút bấm mở Webview từ Bot Mezon.

### Backlog kỹ thuật cho Sprint 0 (không phải story người dùng)
- Seed 500 từ A1–A2 + ≥ 300 câu `quiz_questions` (Trí) → import qua script idempotent (unique `word, part_of_speech`).
- Module map & sơ đồ tuần tự `/quiz` và `Duel` (Công) → cập nhật ARCHITECTURE_DESIGN.md.

---

## 2. ĐẶC TẢ CƠ SỞ DỮ LIỆU CHO MINH (8 MODULES CHUẨN HOÁ — 45 BẢNG)

> **Toàn bộ script SQL thực thi:** Lưu trữ tại [docs/database/sql/](file:///c:/Study/HocKy6/MezonCampusStudio/docs/database/sql) gồm 8 file đánh số thứ tự từ `user.sql` đến `analytics.sql`.  
> **File DBML trực quan:** Dán [docs/database/lingual_full_schema.dbml](file:///c:/Study/HocKy6/MezonCampusStudio/docs/database/lingual_full_schema.dbml) vào [dbdiagram.io](https://dbdiagram.io) để xuất ảnh ERD nộp bài cho Mentor Mai Hồng Mận.

### 2.1 Quy ước chung & Kiến trúc Thiết kế
- **PostgreSQL 13+ / 16**, toàn bộ tên bảng/cột **snake_case** (EF Core: `UseSnakeCaseNamingConvention()`).
- Khóa chính **PK `uuid`** (`gen_random_uuid()`), riêng `srs_reviews` dùng `uuid`, `xp_ledger` dùng `uuid` kèm `idempotency_key` (chống race condition).
- Thời gian dùng **`timestamptz`** (UTC). Ngày hoạt động/streak dùng `date` theo múi giờ `Asia/Ho_Chi_Minh`.
- **Kiến trúc Single-Clan Mezon Bot:** Bot phục vụ riêng cho **1 Clan cụ thể** thông qua bảng singleton `bot_configuration` (ràng buộc `CHECK (id = 1)`). Loại bỏ bảng `clans` đa cấp phức tạp.
- **Mô hình Sổ cái XP Ledger (Append-only Ledger):** Mọi điểm thưởng đều ghi nhận vào `xp_ledger` kèm `idempotency_key` chống gian lận và cộng lặp.
- **Thuật toán SM-2 chuẩn 4 mức:** `again`, `hard`, `good`, `easy` với `ease_factor >= 1.30`.
- **AI Tutor Context:** Lưu cấu trúc sửa lỗi ngữ pháp `correction_payload JSONB` và quản lý `input_tokens`, `output_tokens` của Google Gemini API.

### 2.2 Chiến lược Phân kỳ Migration cho bạn Minh (20 Bảng Core MVP vs 25 Bảng Nâng cao)
Nhóm phân định rõ ràng để Minh BE không bị quá tải khi code trong 10 tuần:
- **Mốc 12/10 (Milestone 1 nộp bài):** Nộp trọn vẹn bản thiết kế **8 Module (45 bảng)** trên `dbdiagram.io` để Mentor đánh giá cao về tầm nhìn kiến trúc hệ thống chuyên nghiệp.
- **Đợt code Migration 1 (Sprint 1–3 — Core MVP ~20 bảng cốt lõi):**
  1. *Module 01 Identity (3 bảng):* `users`, `roles`, `user_roles`.
  2. *Module 02 Curriculum (4 bảng):* `courses`, `units`, `lessons`, `vocabulary_items`, `lesson_vocabulary`.
  3. *Module 03 Learning (5 bảng):* `lesson_progress`, `srs_cards`, `srs_reviews`, `xp_ledger`, `user_streaks`.
  4. *Module 04 Quiz (3 bảng):* `quizzes`, `quiz_questions`, `quiz_options`.
  5. *Module 05 Community (3 bảng):* `bot_configuration`, `configured_clan_members`, `clan_quiz_sessions`, `clan_quiz_responses`.
  6. *Module 06 Competition (3 bảng):* `duel_matches`, `duel_answers`, `duel_results`.
  7. *Module 07 AI Tutor (1 bảng):* `ai_messages` (hoặc `ai_conversations` + `ai_messages`).
- **Đợt code Migration 2 (Sprint 4–6 — Mở rộng 25 bảng còn lại):**
  - Kích hoạt `placement_attempts`, `placement_answers` (Test đầu vào).
  - Sổ tay cá nhân `user_vocabulary_deck`, ví dụ đa ngữ cảnh `vocabulary_examples`.
  - Lập lịch tự động `bot_schedules`, phân quyền `configured_clan_role_grants`.
  - Bảng xếp hạng tuần đóng băng `leaderboard_weeks`, `user_weekly_leaderboard`, `configured_clan_weekly_stats`.
  - Báo cáo phân tích giữ chân `configured_clan_daily_analytics`, `user_retention_cohorts`.

### 2.2 MODULE IDENTITY

**users**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| mezon_user_id | varchar(64) | N | **UQ** |
| username | varchar(100) | Y | |
| display_name | varchar(100) | N | |
| avatar_url | text | Y | |
| role | varchar(20) | N | default `learner`; CHECK IN (learner, content_admin, super_admin) |
| cefr_level | varchar(2) | N | default `A1`; A1/A2/B1/B2 |
| daily_goal_minutes | smallint | N | default 15 |
| timezone | varchar(50) | N | default `Asia/Ho_Chi_Minh` |
| total_xp | int | N | default 0 |
| current_streak | int | N | default 0 |
| longest_streak | int | N | default 0 |
| streak_freezes | smallint | N | default 1 |
| last_active_date | date | Y | ngày local gần nhất đạt ≥ 20 XP |
| last_login_at | timestamptz | Y | |
| created_at / updated_at | timestamptz | N | default now() |
IX: `ux_users_mezon_user_id` (UQ), `ix_users_total_xp`.

**clans**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| mezon_clan_id | varchar(64) | N | **UQ** |
| name | varchar(150) | N | |
| icon_url | text | Y | |
| default_channel_id | varchar(64) | Y | kênh bot gửi quiz |
| is_active | boolean | N | default true |
| installed_by_user_id | uuid | Y | FK→users.id (SET NULL) |
| created_at / updated_at | timestamptz | N | |

**clan_members**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| clan_id | uuid | N | PK (composite), FK→clans.id CASCADE |
| user_id | uuid | N | PK (composite), FK→users.id CASCADE |
| role | varchar(15) | N | default `member`; CHECK IN (member, moderator, owner) |
| joined_at | timestamptz | N | default now() |
IX: `ix_clan_members_user_id`. Vai trò Moderator gắn theo Clan (không để ở `users.role`).

### 2.3 MODULE LEARNING

**courses**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| title | varchar(200) | N | |
| description | text | Y | |
| cefr_level | varchar(2) | N | |
| source_lang / target_lang | varchar(5) | N | default `vi` / `en` |
| order_index | smallint | N | default 0 |
| status | varchar(12) | N | default `published` |
| created_at | timestamptz | N | |
IX: `(cefr_level, status)`.

**units**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| course_id | uuid | N | FK→courses.id CASCADE |
| title | varchar(200) | N | |
| description | text | Y | |
| order_index | smallint | N | |
| icon | varchar(30) | Y | |
| status | varchar(12) | N | default `published` |
IX: **UQ** `(course_id, order_index)`.

**lessons**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| unit_id | uuid | N | FK→units.id CASCADE |
| title | varchar(200) | N | |
| description | text | Y | |
| order_index | smallint | N | |
| xp_reward | int | N | default 25 |
| status | varchar(12) | N | default `published` |
| created_at | timestamptz | N | |
IX: **UQ** `(unit_id, order_index)`.

**vocabularies**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| lesson_id | uuid | Y | FK→lessons.id SET NULL |
| word | varchar(100) | N | |
| ipa | varchar(100) | Y | |
| part_of_speech | varchar(20) | N | default `other` |
| meaning_vi | varchar(500) | N | |
| example_en / example_vi | text | Y | |
| audio_url / image_url | text | Y | |
| cefr_level | varchar(2) | N | |
| topic | varchar(50) | Y | |
| frequency_rank | int | Y | |
| status | varchar(12) | N | default `published` |
| created_at / updated_at | timestamptz | N | |
IX: **UQ** `(word, part_of_speech)` (import idempotent), `lesson_id`, `(cefr_level, status)`.
Cột khớp file CSV/JSON của Trí: `word, ipa, part_of_speech, meaning_vi, example_en, example_vi, audio_url, cefr_level, topic`.

**quiz_questions**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| vocabulary_id | uuid | N | FK→vocabularies.id CASCADE |
| lesson_id | uuid | Y | FK→lessons.id SET NULL |
| type | varchar(20) | N | default `mcq_meaning` (hoặc `mcq_listening`) |
| prompt | text | N | |
| options | jsonb | N | mảng đúng 4 chuỗi; CHECK `jsonb_array_length(options)=4` |
| correct_index | smallint | N | CHECK BETWEEN 0 AND 3 |
| explanation | text | Y | |
| cefr_level | varchar(2) | N | |
| status | varchar(12) | N | default `published` |
| created_at | timestamptz | N | |
IX: `vocabulary_id`, `(cefr_level, status)` (chọn ngẫu nhiên cho /quiz và Duel).

**user_lesson_progress**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| user_id | uuid | N | PK (composite), FK→users.id CASCADE |
| lesson_id | uuid | N | PK (composite), FK→lessons.id CASCADE |
| status | varchar(12) | N | default `in_progress`; in_progress / completed |
| best_score | smallint | Y | |
| completed_at | timestamptz | Y | |
| updated_at | timestamptz | N | |
IX: `lesson_id`.

**srs_states** (trạng thái SM-2 theo từng cặp user–từ)
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| user_id | uuid | N | FK→users.id CASCADE |
| vocabulary_id | uuid | N | FK→vocabularies.id CASCADE |
| repetitions | int | N | default 0 |
| interval_days | int | N | default 0 |
| ease_factor | numeric(3,2) | N | default 2.50; CHECK ≥ 1.30 |
| next_review_at | timestamptz | N | default now() |
| last_reviewed_at | timestamptz | Y | |
| last_quality | smallint | Y | 0..5 |
| lapses | int | N | default 0 |
| state | varchar(10) | N | default `new`; new / learning / review |
| source | varchar(15) | N | default `lesson`; lesson / bot_learn / bookmark |
| created_at / updated_at | timestamptz | N | |
IX: **UQ** `(user_id, vocabulary_id)`, `(user_id, next_review_at)` = hàng đợi ôn tập.

**srs_review_logs**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | bigint identity | N | PK |
| user_id | uuid | N | FK→users.id CASCADE |
| vocabulary_id | uuid | N | FK→vocabularies.id CASCADE |
| quality | smallint | N | 0..5 |
| button | varchar(5) | N | again / hard / good / easy |
| prev_interval_days / new_interval_days | int | N | |
| prev_ease / new_ease | numeric(3,2) | N | |
| response_time_ms | int | Y | |
| reviewed_at | timestamptz | N | default now() |
IX: `(user_id, reviewed_at)`, `(user_id, vocabulary_id)` (tính Weak Words).

### 2.4 MODULE GAMIFICATION

**xp_events** (sổ cái XP; từ đây dựng lại BXH Redis và tính streak)
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | bigint identity | N | PK |
| user_id | uuid | N | FK→users.id CASCADE |
| clan_id | uuid | Y | FK→clans.id SET NULL |
| source_type | varchar(20) | N | new_word / srs_review / lesson / clan_quiz / duel / streak_bonus |
| source_ref | varchar(64) | Y | match_id, session_id, log id… |
| amount | int | N | CHECK > 0 |
| event_date | date | N | ngày local VN |
| created_at | timestamptz | N | |
IX: `(user_id, event_date)`, `(clan_id, created_at)`; thêm trong migration: **UQ partial** `(user_id, source_type, source_ref) WHERE source_ref IS NOT NULL` để không cộng XP hai lần.

### 2.5 MODULE MEZON BOT & WORD DUEL

**clan_quiz_sessions**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| clan_id | uuid | N | FK→clans.id CASCADE |
| mezon_channel_id | varchar(64) | N | |
| mezon_message_id | varchar(64) | Y | |
| quiz_question_id | uuid | N | FK→quiz_questions.id |
| started_by_user_id | uuid | Y | FK→users.id SET NULL |
| status | varchar(10) | N | default `open`; open / closed |
| started_at | timestamptz | N | |
| ends_at | timestamptz | N | |
| first_correct_user_id | uuid | Y | FK→users.id SET NULL |
IX: `(clan_id, started_at)`; partial index `WHERE status='open'` (migration). Tối đa 1 session open mỗi kênh: **UQ partial** `(mezon_channel_id) WHERE status='open'`.

**clan_quiz_answers**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| session_id | uuid | N | FK→clan_quiz_sessions.id CASCADE |
| user_id | uuid | N | FK→users.id CASCADE |
| selected_index | smallint | N | 0..3 |
| is_correct | boolean | N | |
| response_ms | int | N | server đo |
| answered_at | timestamptz | N | |
IX: **UQ** `(session_id, user_id)` (chặn bấm 2 lần / race condition — đúng điều SP2-06 cần bắt).

**matches**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| clan_id | uuid | Y | FK→clans.id SET NULL |
| mezon_channel_id | varchar(64) | Y | |
| mezon_message_id | varchar(64) | Y | Tin nhắn thách đấu của bot — để sửa nút khi accept/expire, chống bấm lại |
| player1_id | uuid | N | FK→users.id (người thách đấu) |
| player2_id | uuid | N | FK→users.id |
| status | varchar(15) | N | default `pending`; pending / accepted / in_progress / finished / declined / expired / abandoned (`accepted`: đã bấm đồng ý, đang chờ 2 người vào Webview) |
| winner_id | uuid | Y | FK→users.id SET NULL; null = hòa/chưa xong |
| player1_score / player2_score | int | N | default 0 (Tổng điểm tích lũy 5 vòng, 100–200đ/câu đúng) |
| total_rounds | smallint | N | default 5 |
| round_seconds | smallint | N | default 10 |
| end_reason | varchar(20) | Y | completed / forfeit_disconnect / declined / timeout / no_show |
| created_at | timestamptz | N | |
| accepted_at | timestamptz | Y | thời điểm người được mời bấm chấp nhận |
| started_at / finished_at | timestamptz | Y | |
| result_posted_at | timestamptz | Y | thời điểm bot đăng Thẻ vinh danh Clan — chống đăng lặp khi bot retry |
CHECK: `player1_id <> player2_id`; `winner_id IS NULL OR winner_id IN (player1_id, player2_id)`.
IX: `(player1_id, created_at)`, `(player2_id, created_at)`, `(clan_id, finished_at)`.
Thêm trong migration: partial index `(player1_id) WHERE status IN ('pending','accepted','in_progress')` và tương tự cho `player2_id` để kiểm tra tức thì "người chơi đang bận trận khác".

**match_rounds**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| match_id | uuid | N | FK→matches.id CASCADE |
| round_no | smallint | N | 1..5 |
| quiz_question_id | uuid | N | FK→quiz_questions.id |
| sent_at | timestamptz | N | server phát câu hỏi |
IX: **UQ** `(match_id, round_no)`.

**match_answers**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| match_round_id | uuid | N | FK→match_rounds.id CASCADE |
| user_id | uuid | N | FK→users.id |
| selected_index | smallint | Y | null = hết giờ |
| is_correct | boolean | N | default false |
| response_ms | int | N | |
| points | int | N | default 0 |
| answered_at | timestamptz | N | |
IX: **UQ** `(match_round_id, user_id)`.

### 2.6 MODULE AI TUTOR (LINGLING)

**ai_chat_logs**
| Field | Type | Null | Ràng buộc / Index |
|---|---|---|---|
| id | uuid | N | PK |
| user_id | uuid | N | FK→users.id CASCADE |
| mode | varchar(20) | N | default `grammar_check`; grammar_check / roleplay / explain |
| user_message | text | N | câu tiếng Anh người dùng nhập |
| ai_response | text | N | phản hồi từ Gemini API |
| feedback_json | jsonb | Y | cấu trúc lỗi sai, giải thích, gợi ý bản xứ |
| tokens_used | int | Y | theo dõi quota Google Gemini |
| created_at | timestamptz | N | |
IX: `(user_id, created_at)`, `(user_id, mode)`.

### 2.7 Quan hệ chính & Bộ Lược đồ CSDL Chuẩn 8 Module (45 bảng)
> **Nguồn chân lý thiết kế CSDL (Source of Truth):** Xem chi tiết tại [`docs/database/lingual_full_schema.dbml`](file:///c:/Study/HocKy6/MezonCampusStudio/docs/database/lingual_full_schema.dbml) và file khởi tạo [`docs/database/sql/00_init_all.sql`](file:///c:/Study/HocKy6/MezonCampusStudio/docs/database/sql/00_init_all.sql).
- `courses` 1—N `units` 1—N `lessons` 1—N `lesson_vocabulary` N—1 `vocabulary_items` 1—N `vocabulary_examples`.
- `users` 1—1 `learner_profiles`; `users` N—N `roles` qua `user_roles`.
- `users` 1—N `srs_cards` (trạng thái thuật toán SM-2) 1—N `srs_reviews` (nhật ký ôn tập).
- `users` 1—N `lesson_progress`; `users` 1—N `learning_sessions`.
- `users` 1—N `xp_ledger` (sổ cái bất biến Append-Only có `idempotency_key`); `users` 1—1 `user_streaks`.
- `bot_configuration` (Single-Clan Singleton) 1—N `configured_clan_members`, `bot_schedules`, `clan_quiz_sessions` 1—N `clan_quiz_responses`.
- `duel_matches` 1—N `duel_match_questions` (5 câu/trận) 1—N `duel_answers`; `duel_matches` 1—1 `duel_results`.
- `leaderboard_weeks` 1—N `user_weekly_leaderboard` (N—1 `users`).
- `ai_scenarios` 1—N `ai_conversations` 1—N `ai_messages` (kèm tracking `tokens_used`).

### 2.8 Việc để dành cho Redis, không nhét vào RDBMS
BXH tuần (`ZADD`), trạng thái trận đang chạy (heartbeat/reconnect), rate-limit sliding window, cache giải thích AI. Có thể ghi chú một ô "Redis keys" bên cạnh ERD để Mentor thấy ranh giới.

---

## 3. KẾ HOẠCH SPRINT 0 TỪNG NGÀY (04/10 → 12/10/2026)

Nguyên tắc chung theo Sprint Plan: daily bằng `*daily` trên #daily-team05 trước 22:00 (≥ 1 giờ/ngày); blocker > 4 giờ thì tag Công và anh Mai Hồng Mận. Mọi thay đổi tài liệu qua PR vào `develop`, không push thẳng `main`.

Tải việc được chia đủ nhẹ để làm được cả trong tuần có lịch học; ngày nào sát deadline cá nhân thì kéo task "stretch" sang sau.

| Ngày | Công (Lead/Bot/AI) | Minh (DB/BE) | Trí (FE/Data/QA) |
|---|---|---|---|
| **CN 04/10** | Chốt D1–D8, thống nhất phân kỳ Migration 8 module (45 bảng). Đẩy DBML chuẩn vào `/docs/database` | Import `lingual_full_schema.dbml` (đã có TableGroup 8 module) vào dbdiagram.io, kéo thả layout | Chốt template CSV vocab theo cột ở 2.3; chia 500 từ thành 5 lô × 100; làm mẫu 20 từ |
| **T2 05/10** | Sửa PRD → **v1.1**: D1–D7 (nút SRS, 30s, streak, Mezon UX, AI log P0/P1). Thêm mục "Quy tắc điểm Duel" | **ERD v1**: chốt các cột mới của `matches` (accepted, no_show, result_posted_at), đủ 18 bảng. Review với Công | Lô 1: 100 từ A1 (Unit 1–2) + 30 `quiz_questions` mẫu khớp schema |
| **T3 06/10** | Viết lại ARCHITECTURE_DESIGN.md theo .NET 8 (module map 7 project, SignalR, Adapter Mezon, sơ đồ tuần tự `/quiz` & Duel). Thêm mục **Spike Mezon SSO & Webview** | Viết **data dictionary** (đã có nền ở phần 2) + bảng quan hệ; liệt kê CHECK/partial index cần đưa vào migration (để Sprint 1 làm ngay) | Lô 2: 100 từ A1 (còn lại) + viết script kiểm tra CSV (trùng từ, thiếu cột, `cefr_level` hợp lệ) |
| **T4 07/10** | Sprint Plan → **v1.1**: sửa SP2-03 (30s), SP3-01 (thách đấu trước, ghép cặp stretch), thêm story ID từ phần 1 vào backlog | **Ứng viên freeze ERD**: xuất PNG/PDF + giữ DBML trong `/docs/database` | Lô 3: 100 từ A2 + 60 câu quiz; chạy script kiểm tra |
| **T5 08/10** | **Gửi Mentor** bản xem trước: ERD v1 + PRD v1.1 + Sprint Plan v1.1, xin phản hồi trong 24h. Xác nhận cách nộp (kênh, định dạng) | Chuẩn bị kế hoạch seed 500 từ + sơ đồ migration thứ tự bảng (cho SP1-01) | Lô 4: 100 từ A2 + 60 câu quiz; viết **Test Plan** (US-101…US-209, mỗi story ≥ 1 test case) |
| **T6 09/10** | Kiểm chứng scaffold: `dotnet build`, `npm run build`, `docker compose up -d`, `/health`, Swagger — chụp màn hình làm bằng chứng. Xử lý phản hồi Mentor đợt 1 | Sửa ERD theo phản hồi Mentor (nếu có) | Lô 5: 100 từ A2 cuối + 60 câu quiz; hoàn thiện Test Plan |
| **T7 10/10** | Hoàn thiện PRD/Sprint Plan bản cuối, xuất `.docx` A4 (+ `.md`), kiểm tra mục lục/sơ đồ | **ERD v1.0 final** + chốt thay đổi sau phản hồi; commit DBML | Hoàn tất **500 từ** (JSON + CSV), chạy script kiểm tra sạch lỗi; ≥ 300 câu quiz |
| **CN 11/10** | Dry-run: Công đóng vai Mentor đọc cả gói, đối chiếu PRD ↔ ERD ↔ Sprint Plan (đúng 18 bảng, phân kỳ 13+5, nút SRS, timer, streak, luồng Mezon UX, hàng AI) | Dry-run: tự kiểm checklist ERD ở DoD bên dưới | Dry-run: kiểm checklist dữ liệu + bằng chứng build |
| **T2 12/10** | **Nộp Milestone 1** sớm trong ngày (không chờ sát giờ); tag Mentor trên kênh; gắn git tag `milestone-1` | Có mặt hỗ trợ nếu Mentor hỏi về ERD | Có mặt hỗ trợ; lưu bản nộp |

Task có thể làm thêm nếu dư sức (stretch, không ảnh hưởng nộp): Minh dựng sẵn khung entity EF Core cho `users`/`vocabularies`/`srs_states` (đầu SP1-01); Công dựng echo test cho Mezon Bot webhook (đầu SP1-05); Trí dựng khung màn Flashcard với dữ liệu mock.

### 3.1 DEFINITION OF DONE — MILESTONE 1

**Database ERD (Minh · A: Công)**
- [ ] ERD hiển thị đủ 18 bảng, có PK, FK, kiểu dữ liệu, nullable, index; xuất PNG/PDF + file DBML trong repo.
- [ ] Mỗi bảng trong 8 bảng yêu cầu của đề bài đều có mặt; tên khớp PRD/Sprint Plan (ghi chú nếu đổi tên).
- [ ] Có data dictionary và danh sách CHECK/partial index cần đưa vào migration.
- [ ] Có thể trả lời được: SM-2 lưu ở đâu, XP tính từ đâu, trận Duel lưu thế nào, chống bấm hai lần ra sao.

**PRD + Sprint Plan (Công)**
- [ ] PRD v1.1 và Sprint Plan v1.1 `.docx` A4; hai tài liệu thống nhất D1–D5.
- [ ] Phạm vi MVP ghi rõ hai luồng cốt lõi; user stories P0 có AC kiểm chứng được.
- [ ] ARCHITECTURE_DESIGN.md khớp .NET 8 (không bắt buộc nộp nhưng nên sửa trước khi Mentor đọc).

**Nền tảng kỹ thuật (Công)**
- [ ] `dotnet build` 0 error; `npm run build` pass; `docker compose up -d` chạy Postgres 16 + Redis 7; `/health` và Swagger hoạt động.
- [ ] Không có secret trong repo (`.env.example`, không commit `.env`).

**Dữ liệu (Trí)**
- [ ] 500 từ A1–A2 (JSON + CSV) qua script kiểm tra: không trùng `(word, part_of_speech)`, không thiếu cột bắt buộc.
- [ ] ≥ 300 câu quiz có 4 đáp án, `correct_index`, giải thích.
- [ ] Test Plan phủ US-101…US-209.

**Quy trình**
- [ ] Mọi thay đổi đã merge qua PR (≥ 1 phê duyệt); daily đủ các ngày.
- [ ] Đã gửi Mentor bản xem trước từ 08/10 và ghi nhận phản hồi (đã sửa hoặc có lý do chưa sửa).
- [ ] Bản nộp đã gắn tag `milestone-1`, gửi đúng kênh/định dạng Mentor yêu cầu.

---

## 4. RỦI RO RIÊNG CHO 8 NGÀY NÀY
- **Lịch học/thi cá nhân:** nếu một người trượt kế hoạch, ưu tiên thứ tự: ERD → PRD/Sprint Plan đồng nhất → build xanh → dữ liệu 500 từ (có thể nộp 300 từ + kế hoạch hoàn tất, báo Mentor).
- **Mentor phản hồi muộn:** gửi bản xem trước ngày 08/10 để còn 3 ngày sửa; nếu chưa có phản hồi đến 10/10 thì nộp bản hiện có và ghi chú "chờ review".
- **Lệch tài liệu:** sai khác nhỏ giữa PRD và ERD (tên bảng, số nút, timer) dễ bị hỏi nhất; bước dry-run ngày 11/10 dành riêng để quét điểm này.
