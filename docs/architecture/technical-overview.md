# 🛠️ LINGUAL — TỔNG QUAN KỸ THUẬT & CÔNG NGHỆ (TECHNICAL OVERVIEW)
## DỰ ÁN: NỀN TẢNG HỌC NGOẠI NGỮ CỘNG ĐỒNG TRÊN MEZON PLATFORM

> **Chương trình:** Mezon Campus Studio 2026 (NCC+ & Mezon Platform)  
> **Đội ngũ (Team 05 — Đụt Cận Trĩ):** Ngô Văn Công (Lead), Nguyễn Công Minh (DB/Backend), Phan Phước Trí (Frontend/QA)  
> **Mentor phụ trách:** Mai Hồng Mận  
> **Phiên bản:** v1.1 CHÍNH THỨC (05/10/2026)  

---

## 1. TECH STACK CHÍNH THỨC

### 1.1 Backend Core (.NET 8 Modular Monolith)
- **Framework:** ASP.NET Core 8 Web API (C# 12).
- **Kiến trúc:** Modular Monolith tổ chức phân tách theo domain context:
  - `Lingual.Domain`: Entities, Value Objects, Domain Events.
  - `Lingual.Application`: CQRS Handlers, Use Cases, SM-2 Engine, Idempotent XP Services.
  - `Lingual.Infrastructure`: EF Core 8, Redis 7 Client, Gemini Client, Mezon Webhook Adapter.
  - `Lingual.Api`: Web API Host, Swagger, JWT Bearer Middleware, Rate Limiting.
  - `Lingual.Realtime`: SignalR GameHub cho đấu trường Word Duel 1vs1.
  - `Lingual.Bot`: Mezon Bot Service Worker lắng nghe webhook & cron scheduler.
- **ORM & Data Access:** Entity Framework Core (EF Core 8) + Npgsql.
- **In-Memory Cache & State:** Redis 7 (StackExchange.Redis) cho Sliding Window Rate Limiting và SignalR game sessions.

### 1.2 Frontend & Channel Mini-App (Next.js 14)
- **Framework:** Next.js 14 (App Router) với React 19.
- **Styling & UI:** Tailwind CSS, Lucide React Icons, Canvas Confetti.
- **Realtime Client:** `@microsoft/signalr` kết nối GameHub qua WebSockets.
- **Tích hợp Mezon Client:** `mezonBridge` JS SDK bắt sự kiện SSO handshake và theme mode (Dark/Light).

### 1.3 Cơ sở Dữ liệu & Lưu trữ (PostgreSQL 16)
- **Phiên bản:** PostgreSQL 16 (chạy Docker `postgres:16-alpine`).
- **Quy mô:** **22 bảng Core MVP** chia làm 9 module logic.
- **Đặc tính kỹ thuật cốt lõi:**
  - Extension bắt buộc: `CREATE EXTENSION IF NOT EXISTS pgcrypto;` cho `gen_random_uuid()`.
  - Tự động cập nhật `updated_at`: Trigger PL/pgSQL `set_updated_at()` trên 11 bảng có trường thời gian sửa đổi.
  - Sổ cái tài chính gamification: `xp_ledger` là bảng Append-Only có ràng buộc `uq_xp_ledger_source` và `idempotency_key` chống cộng trùng điểm.
  - Gộp trường linh động qua JSONB: `options` trong câu hỏi, `session_history` trong tiến độ bài học, `review_history` trong thẻ SRS, `schedules` trong cấu hình Bot, `messages` trong hội thoại AI.
  - Kiểm toán bất biến: Bảng `audit_logs` (Module 09) ghi nhận lịch sử thay đổi phân quyền và cấu hình nhạy cảm.

### 1.4 Trí tuệ nhân tạo (Google Gemini API)
- **Mô hình:** Google Gemini 1.5 Flash (tối ưu tốc độ phản hồi < 1s, chi phí thấp).
- **Vai trò:** Đóng vai Mascot bò sữa LingLing, phân tích sửa lỗi ngữ pháp/chính tả và luyện đối thoại phản xạ theo tình huống thực tế.

---

## 2. TIÊU CHUẨN AN TOÀN & BẢO MẬT (NCC+ COMPLIANCE)

1. **Xác thực chữ ký số Webhook (`X-Mezon-Signature`):**
   - Mọi webhook từ Mezon Gateway gửi về đều chứa chữ ký HMAC SHA-256.
   - Middleware `MezonHmacValidator` tính toán hash từ request body với `MEZON_WEBHOOK_SECRET` trước khi xử lý.
2. **Quản lý Định danh & Simple RBAC:**
   - Đăng nhập xác thực qua Mezon SSO OAuth2; API phát hành JWT ngắn hạn (15 phút).
   - Phân quyền toàn cục trực tiếp qua cột `users.role` (`learner`, `moderator`, `admin`).
   - Phân quyền Clan Moderator tách biệt qua bảng `clan_moderator_grants`.
3. **Chống Spam & Gian lận:**
   - Sliding-window Rate Limiting trên ASP.NET Core & Redis (tối đa 5 req/s/user).
   - Kiểm tra thời gian tối thiểu hoàn thành bài học (chống bot speed-run).
4. **Không lưu trữ Secret dạng Plaintext:**
   - Mọi credential, OAuth secret, Gemini API Key được nạp qua biến môi trường (`.env`).
   - Quyền `UPDATE` và `DELETE` trên bảng `audit_logs` bị thu hồi khỏi role ứng dụng trong production.
