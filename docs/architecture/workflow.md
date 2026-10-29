# 🔄 LINGUAL — ĐẶC TẢ QUY TRÌNH HỆ THỐNG (SYSTEM WORKFLOWS)
## DỰ ÁN: NỀN TẢNG HỌC NGOẠI NGỮ CỘNG ĐỒNG TRÊN MEZON PLATFORM

> **Chương trình:** Mezon Campus Studio 2026 (NCC+ & Mezon Platform)  
> **Đội ngũ (Team 05 — Đụt Cận Trĩ):** Ngô Văn Công (Lead), Nguyễn Công Minh (DB/Backend), Phan Phước Trí (Frontend/QA)  
> **Mentor:** Mai Hồng Mận  
> **Phiên bản:** v1.1 (Đồng bộ CSDL 22 bảng Core MVP)  

---

## 1. QUY TRÌNH KHỞI TẠO TÀI KHOẢN & ĐÁNH GIÁ ĐẦU VÀO (ONBOARDING)

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Học viên
    participant Webview as Next.js 14 Mini-App
    participant API as ASP.NET Core API
    participant OAuth as Mezon SSO
    participant DB as PostgreSQL 16

    Learner->>Webview: Mở tab Lingual trong kênh Mezon
    Webview->>OAuth: Lấy mezon_auth_token từ client context
    OAuth-->>Webview: auth_token
    Webview->>API: POST /api/v1/auth/mezon {token}
    API->>DB: UPSERT INTO users (mezon_user_id, role='learner')
    API->>DB: Khởi tạo learner_profiles & user_streaks nếu chưa có
    API-->>Webview: JWT Bearer Token + Onboarding Status

    alt Người học mới chưa hoàn thành Onboarding
        Learner->>Webview: Chọn mục tiêu & thời lượng cam kết (5/15/30 phút)
        Learner->>Webview: Làm bài test 10 câu (Placement Test) hoặc Bỏ qua
        Webview->>API: POST /api/v1/onboarding/placement {answers_detail}
        API->>DB: INSERT INTO placement_tests
        API->>DB: UPDATE learner_profiles SET initial_cefr_level, onboarding_completed=true
        API-->>Webview: Kết quả phân lớp (A1/A2/B1/B2) & Khóa học đề xuất
    end
```

---

## 2. QUY TRÌNH HỌC TẬP & ÔN TẬP NGẮT QUÃNG (SRS SM-2)

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Học viên
    participant App as Mini-App Webview
    participant API as Backend API
    participant DB as PostgreSQL 16

    Learner->>App: Chọn bài học từ Lộ trình (Lessons)
    App->>API: GET /api/v1/lessons/{id}
    API->>DB: SELECT lessons + vocabulary_items (theo vocabulary_ids JSONB)
    API-->>App: Dữ liệu từ vựng, phiên âm IPA, ví dụ song ngữ
    
    Learner->>App: Hoàn thành bài học
    App->>API: POST /api/v1/learning/lessons/{id}/complete
    API->>DB: UPDATE lesson_progress (session_history JSONB)
    API->>DB: INSERT INTO srs_cards (Khởi tạo thẻ SRS cho các từ mới)
    API->>DB: INSERT INTO xp_ledger (+25 XP, idempotency_key)
    API->>DB: UPDATE user_streaks (Kiểm tra điều kiện >= 20 XP để tăng Streak)
    API-->>App: Thẻ chúc mừng, +25 XP, Cập nhật Streak

    opt Ôn tập Flashcard đến hạn (SRS Review)
        Learner->>App: Mở mục Ôn tập hàng ngày
        App->>API: GET /api/v1/learning/srs/due
        API->>DB: SELECT srs_cards WHERE next_review_at <= now()
        API-->>App: Danh sách thẻ cần ôn
        Learner->>App: Đánh giá chất lượng nhớ (Again / Hard / Good / Easy)
        App->>API: POST /api/v1/learning/srs/review {card_id, rating}
        API->>DB: UPDATE srs_cards (Tính lại interval, ease_factor, ghi review_history JSONB)
        API->>DB: INSERT INTO xp_ledger (+3 XP nếu Good/Easy)
        API-->>App: Lịch ôn tiếp theo cho từ vựng
    end
```

---

## 3. QUY TRÌNH ĐỐ VUI CỘNG ĐỒNG CLAN (BOT CHAT QUIZ)

```mermaid
sequenceDiagram
    autonumber
    actor Mod as Clan Moderator / Cron
    actor ClanMembers as Thành viên Clan
    participant Bot as Mezon Bot
    participant API as Backend API
    participant DB as PostgreSQL 16

    Mod->>Bot: Kích hoạt /quiz hoặc Lịch tự động 20:00 (bot_configuration.schedules)
    Bot->>API: POST /api/v1/community/quiz/start
    API->>DB: SELECT ngẫu nhiên từ quiz_questions (status='published')
    API->>DB: INSERT INTO clan_quiz_sessions (status='in_progress')
    API-->>Bot: Câu hỏi, 4 đáp án và thời gian đếm ngược (30 giây)
    Bot->>ClanMembers: Gửi tin nhắn đố vui kèm các nút bấm A, B, C, D

    ClanMembers->>Bot: Bấm chọn đáp án
    Bot->>API: POST /api/v1/community/quiz/answer {session_id, user_id, answer, response_ms}
    API->>DB: Cập nhật responses (JSONB) trong clan_quiz_sessions

    Note over Bot,API: Hết 30 giây thời gian quy định
    API->>DB: UPDATE clan_quiz_sessions (status='completed')
    API->>DB: Ghi sổ cái xp_ledger (+10 XP người đúng, +5 XP người nhanh nhất < 5s)
    API-->>Bot: Danh sách người chiến thắng & Thống kê
    Bot->>ClanMembers: Công bố đáp án đúng, giải thích từ vựng và bảng vinh danh top tốc độ!
```

---

## 4. QUY TRÌNH ĐẤU TRƯỜNG TỪ VỰNG 1VS1 (WORD DUEL 3 BƯỚC)

1. **Khởi xướng:** Gõ `/duel @user` trên kênh chat Clan ➔ Bot gửi tin nhắn thách đấu kèm 2 nút bấm tương tác **[Chấp nhận]** / **[Từ chối]**.
2. **Đấu trường:** Khi chấp nhận, cả 2 chuyển sang màn hình **Channel Mini-App (Webview nhúng Mezon)** kết nối SignalR GameHub để thi đấu 5 vòng đối kháng 10s/vòng (không spam kênh chat, âm thanh mượt mà, server là nguồn sự thật).
3. **Vinh danh:** Kết thúc trận, Bot tự động gửi **Thẻ vinh danh kết quả kèm XP** vào lại kênh chat Clan để thúc đẩy phong trào.
*(Chi tiết xem sơ đồ tuần tự tại `ARCHITECTURE_DESIGN.md`).*

---

## 5. QUY TRÌNH LUYỆN NÓI CÙNG TRỢ LÝ AI LINGLING (GEMINI FLASH)

```mermaid
sequenceDiagram
    autonumber
    actor Learner as Học viên
    participant MiniApp as Mini-App Webview
    participant API as Backend API
    participant Gemini as Google Gemini 1.5 Flash
    participant DB as PostgreSQL 16

    Learner->>MiniApp: Chọn chủ đề đóng vai (Đi cafe, Phỏng vấn xin việc)
    MiniApp->>API: POST /api/v1/ai/conversations {scenario_id}
    API->>DB: INSERT INTO ai_conversations (messages JSONB rỗng)
    API-->>MiniApp: Khởi tạo phiên trò chuyện với LingLing

    Learner->>MiniApp: Nhập tin nhắn tiếng Anh: "I want a coffee please"
    MiniApp->>API: POST /api/v1/ai/conversations/{id}/messages
    API->>Gemini: Gửi System Prompt + Ngữ cảnh hội thoại gần nhất
    Gemini-->>API: Phản hồi đối thoại + Nhận xét ngữ pháp + Gợi ý tự nhiên hơn
    API->>DB: UPDATE ai_conversations (Ghi thêm tin nhắn vào messages JSONB)
    API-->>MiniApp: Hiển thị phản hồi từ LingLing & Khung sửa lỗi ngữ pháp
```
