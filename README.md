# 🚀 LINGUAL (LINGUAMEZON) — MEZON CAMPUS STUDIO 2026

[![Program](https://img.shields.io/badge/Program-Mezon_Campus_Studio_2026-6366f1?style=for-the-badge)](https://campus.mezon.vn)
[![Organizer](https://img.shields.io/badge/Organizer-NCC%2B%20%7C%20Mezon-06b6d4?style=for-the-badge)](https://mezon.ai)
[![Project](https://img.shields.io/badge/Project-Lingual_Language_Learning-10b981?style=for-the-badge)](#)
[![Milestone](https://img.shields.io/badge/Milestone-M1:_PRD_&_Schema_22_Tables-f59e0b?style=for-the-badge)](#)
[![Build](https://img.shields.io/badge/Build-Passing-22c55e?style=for-the-badge)](#)

> **Dự án:** LINGUAL — Social & Gamified Language Learning Platform on Mezon  
> **Đơn vị:** Team 05 — Khoa Công nghệ Thông tin, Trường Đại học Bách Khoa – Đại học Đà Nẵng  
> **Mentor Hướng dẫn:** Anh **Mai Hồng Mận** (`man.maihong` — NCC+)  
> **Project Lead:** **Ngô Văn Công** (`cong.ngovan`)  
> **Tech Stack chính thức:** **ASP.NET Core 8 (Modular Monolith)** + **Next.js 14 (App Router)** + **PostgreSQL 16 & Redis 7**  
> **Workspace:** `C:\Study\HocKy6\MezonCampusStudio`  
> **Git Repositories:**  
> - **Repo chính thức BTC:** [`mezon-campus-studio-06-2026-ut-can-tri`](https://github.com/mezon-campus-studio/mezon-campus-studio-06-2026-ut-can-tri) (Nhánh phát triển: `sprint1`)  
> - **Repo Audit & Review:** [`vcongggggg/lingual-audit`](https://github.com/vcongggggg/lingual-audit) (Nhánh: `main`)  
> **Phương pháp quản lý:** Agile/Scrum Weekly Sprint, Daily Standup & Security Audit theo chuẩn NCC+  

---

## 👥 1. THÀNH VIÊN ĐỘI NGŨ & PHÂN CÔNG TRÁCH NHIỆM

| Thành viên | Tài khoản | Vai trò chính | Trọng tâm Phụ trách |
| :--- | :---: | :---: | :--- |
| **Ngô Văn Công** | `cong.ngovan` | **Project Lead / Realtime & AI** | Kiến trúc hệ thống Modular Monolith, tích hợp Bot Mezon Gateway, SignalR GameHub thi đấu thời gian thực, AI Tutor LingLing (Google Gemini API). |
| **Nguyễn Quang Minh** | `minh.nguyen` | **Backend & Database Lead** | Thiết kế CSDL 9 module (22 bảng Core MVP), Entity Framework Core 8, thuật toán Spaced Repetition (SRS SM-2), logic chuỗi Streak & sổ cái XP Ledger. |
| **Nguyễn Hữu Trí** | `tri.nguyen` | **Frontend & QA Lead** | Giao diện Next.js 14 App Router, Webview nhúng Mezon, tương tác Flashcard 3D, chuẩn hóa bộ dữ liệu 500 từ vựng A1-A2, xây dựng Test Plan & kiểm thử hệ thống. |

---

## 📂 2. CẤU TRÚC THƯ MỤC DỰ ÁN (PROJECT LAYOUT)

```text
MezonCampusStudio/
├── docker-compose.yml         # Môi trường chạy local: PostgreSQL 16 Alpine & Redis 7 Alpine
├── .env.example               # Mẫu biến môi trường (Mezon App ID, Secret, Bot Token, Gemini Key)
├── .gitignore                 # Bỏ qua mã rác, secrets, build artifacts (bin/obj, node_modules)
├── README.md                  # Tài liệu tổng quan dự án (file này)
├── docs/                      # Tài liệu kỹ thuật, kiến trúc & quy trình Agile
│   ├── database/              # [CSDL CHÍNH THỨC] Lược đồ CSDL 9 Module — 22 Bảng Core MVP
│   │   ├── sql/               # 9 file DDL module hóa (user.sql -> audit.sql)
│   │   │   ├── 00_init_all.sql # Script DDL gộp khởi tạo toàn bộ CSDL 22 bảng chuẩn PostgreSQL
│   │   │   └── README.md      # Hướng dẫn Concurrency, Atomic Appends & Quy chuẩn triển khai
│   │   ├── lingual_full_schema.dbml # Lược đồ quan hệ đầy đủ xem trên dbdiagram.io
│   │   └── database.zip       # Gói nén lưu trữ toàn bộ SQL & DBML phục vụ nộp bài
│   ├── product/               # Tài liệu Nghiệp vụ & Kế hoạch Sprint
│   │   ├── PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.md   # [PRD v1.1] Đặc tả yêu cầu sản phẩm
│   │   ├── PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx # PRD bản Word A4 chuẩn in ấn/nộp bài
│   │   ├── SPRINT_PLAN_LINGUAL_MCS2026.md             # [Sprint Plan v1.1] Kế hoạch 10 tuần chi tiết
│   │   ├── SPRINT_PLAN_LINGUAL_MCS2026.docx           # Sprint Plan bản Word A4 có nhúng sơ đồ
│   │   ├── LINGUAL_MILESTONE1_PACK.md                 # Hồ sơ tổng hợp bàn giao Milestone 1
│   │   └── SPRINT_ROADMAP.md                          # Lộ trình tổng quan lịch sử chương trình
│   └── assets/                # Sơ đồ kiến trúc, tiến độ Gantt, ma trận 3 luồng song song
├── src/                       # Mã nguồn hệ thống
│   ├── backend/               # ASP.NET Core 8 Modular Monolith Solution
│   │   ├── Lingual.sln        # Visual Studio / .NET Solution
│   │   ├── Lingual.Api/       # Web API Host, Swagger, SignalR GameHub, HealthChecks
│   │   ├── Lingual.Shared/    # BaseEntity, ApiResponse, Shared Kernel, DTOs
│   │   ├── Lingual.Modules.Identity/     # Mezon SSO OAuth2, JWT Bearer, RBAC
│   │   ├── Lingual.Modules.Learning/     # Khóa học, bài học, từ vựng, SRS SM-2 Engine
│   │   ├── Lingual.Modules.Gamification/ # XP Ledger, Streak, BXH Redis, Word Duel SignalR Hub
│   │   ├── Lingual.Modules.MezonBot/     # Bot Mezon Gateway, Slash commands (/quiz, /duel)
│   │   └── Lingual.Modules.AITutor/      # Mascot LingLing, Google Gemini API
│   │
│   └── web-app/               # Next.js 14 App Router (Tailwind CSS, Lucide Icons, SignalR)
│       ├── src/app/           # Layouts, Routes (Dashboard, Học từ vựng, Đấu trường, Hồ sơ)
│       ├── src/components/    # UI Components tương tác (Flashcard 3D, Duel Arena Webview)
│       └── package.json       # Dependencies
├── scripts/                   # Công cụ tự động hóa & biên dịch tài liệu
│   ├── build_prd_docx.js      # Biên dịch PRD từ Markdown sang file Word (.docx) chuẩn A4
│   ├── build_sprint_docx.js   # Biên dịch Sprint Plan sang file Word (.docx) có nhúng sơ đồ
│   ├── verify_env.py          # Kiểm tra môi trường phát triển (.NET 8, Node.js, Docker)
│   └── daily_report.py        # Tự động xuất Daily Standup & gửi thông báo về Telegram
└── tests/                     # Bộ kiểm thử Unit & Integration Tests (xUnit)
```

---

## 🗄️ 3. KIẾN TRÚC CƠ SỞ DỮ LIỆU (9 MODULES — 22 BẢNG CORE MVP)

Hệ thống áp dụng triết lý **Gộp dữ liệu bằng JSONB Flex Payloads** theo định hướng của Mentor Mai Hồng Mận, giúp tinh gọn hơn 50% số bảng cồng kềnh mà vẫn bảo toàn 100% nghiệp vụ và tính toàn vẹn:

| Module | Tên Module | Số bảng | Danh sách các Bảng Cốt lõi & Đặc tả |
| :---: | :--- | :---: | :--- |
| **01** | **Identity & Onboarding** | **3** | `users`, `learner_profiles`, `placement_tests` (answers_detail JSONB). |
| **02** | **Curriculum & Vocab** | **4** | `courses`, `units`, `lessons` (vocabulary_ids JSONB), `vocabulary_items` (examples JSONB). |
| **03** | **Learning & Gamification** | **4** | `lesson_progress` (session_history JSONB), `srs_cards` (review_history JSONB), `xp_ledger` (sổ cái bất biến có idempotency_key), `user_streaks` (freeze_history, daily_activity JSONB). |
| **04** | **Quiz & Assessment** | **3** | `quizzes`, `quiz_questions` (options JSONB), `quiz_attempts` (answers_detail JSONB). |
| **05** | **Community (Single-Clan)** | **3** | `bot_configuration` (singleton cấu hình clan/bot), `clan_quiz_sessions` (responses JSONB), `clan_moderator_grants` (phân quyền clan). |
| **06** | **Competition & Duel** | **2** | `duel_matches` (gộp câu hỏi, bài làm và điểm số JSONB), `weekly_leaderboards`. |
| **07** | **AI Tutor (LingLing)** | **2** | `ai_scenarios`, `ai_conversations` (messages JSONB, quản lý quota Gemini API). |
| **08** | **Analytics & Reporting** | **0** | *Phase 2 (Truy vấn tối ưu trực tiếp từ `xp_ledger` và `lesson_progress`).* |
| **09** | **Audit Trail** | **1** | `audit_logs` (nhật ký kiểm toán an toàn append-only cho Admin và Clan Moderator). |
| **Tổng** | **Core MVP** | **22** | **22 bảng chuẩn hóa PostgreSQL 13+** (11 trigger updated_at, 2 trigger validate JSONB, 31 indexes). |

---

## 🛠️ 4. BỘ CÔNG NGHỆ CHÍNH THỨC (OFFICIAL TECH STACK)

* **Backend Lõi:** **ASP.NET Core 8 (Kiến trúc Modular Monolith)**:
  - **Modules:** `Modules.Identity`, `Modules.Learning` (SRS SM-2), `Modules.Gamification` (SignalR Realtime Duels), `Modules.MezonBot`, `Modules.AITutor`.
  - **ORM & Data:** Entity Framework Core (EF Core 8) + LINQ Type-safe.
  - **Realtime Game:** ASP.NET Core SignalR (1vs1 Word Duel & Live Clan Quiz).
  - **Xử lý Đồng thời:** Toán tử raw SQL nguyên tử `||` trên các mảng JSONB đa-writer để chống Lost Update.
* **Frontend / Channel Mini-App:** **Next.js 14 (App Router)**, React 19, TailwindCSS, Lucide Icons.
* **Mezon Platform:** `mezon-sdk` (TypeScript Gateway), Mezon Gateway WebSockets, Webhooks, Single-Clan Bot Architecture.
* **Cơ sở dữ liệu & Cache:** PostgreSQL 16 Alpine (RDBMS), Redis 7 Alpine (Sorted Sets cho Realtime Leaderboards & Sliding-window Rate Limiting).
* **Trí tuệ nhân tạo (AI):** Google Gemini 1.5 Flash (sửa lỗi ngữ pháp P0 & đóng vai hội thoại Mascot LingLing P1).
* **Bảo mật & Audit (Chuẩn NCC+):** ASP.NET Core Built-in Rate Limiting (`SlidingWindow`), JWT Auth, Webhook HMAC Signature Verification, FluentValidation.
* **DevOps & QA:** Docker / Docker Compose, GitHub Actions CI/CD, xUnit (.NET).

---

## ⚡ 5. HƯỚNG DẪN KHỞI CHẠY NHANH (QUICK START)

### 5.1 Khởi động Hạ tầng Database & Cache (Docker):
```bash
docker compose up -d
```
*(Khởi chạy PostgreSQL 16 tại cổng `5432` và Redis 7 tại cổng `6379`).*

Khởi tạo CSDL 22 bảng:
```bash
# Nạp toàn bộ schema vào PostgreSQL:
docker exec -i lingual-postgres psql -U lingual_user -d lingual_db < docs/database/sql/00_init_all.sql
```

### 5.2 Khởi động Backend (ASP.NET Core 8):
```bash
cd src/backend
dotnet restore
dotnet run --project Lingual.Api
```
- Swagger API Docs: [http://localhost:5000](http://localhost:5000) (hoặc `http://localhost:5239`)
- SignalR GameHub: `/hubs/game`
- Health Check: `/health`

### 5.3 Khởi động Frontend (Next.js 14):
```bash
cd src/web-app
npm install
npm run dev
```
- Giao diện người dùng: [http://localhost:3000](http://localhost:3000)

### 5.4 Biên dịch Báo cáo & Tài liệu Word (.docx):
```bash
# Xuất PRD chuẩn A4:
node scripts/build_prd_docx.js

# Xuất Sprint Plan chuẩn A4 có nhúng sơ đồ:
node scripts/build_sprint_docx.js
```

---

## 📅 6. TIẾN ĐỘ & CÁC MỐC QUAN TRỌNG (MILESTONES ROADMAP)

- [x] **Milestone 1 — Sprint 0: Khởi động, PRD & Thiết kế CSDL** *(Hạn chót: 12/10/2026)*:
  - Bản vẽ CSDL ERD 9 module — 22 bảng Core MVP hoàn chỉnh (`00_init_all.sql` & `lingual_full_schema.dbml`).
  - Tài liệu PRD v1.1 (`.md` & `.docx`) phê duyệt bởi Mentor Mai Hồng Mận.
  - Kế hoạch Sprint Plan v1.1 (`.md` & `.docx`) phân rã chi tiết 10 tuần.
  - Khung scaffold .NET 8 Modular Monolith và Next.js 14 build xanh 100%.
- [ ] **Milestone 2 — Sprint 1 & 2: Core Learning Engine & Interactive Mezon Bot v1.0** *(Tuần 3 – Tuần 6)*:
  - Mezon SSO OAuth2, Flashcard 3D học từ vựng thuật toán SM-2, tích lũy Streak & XP Ledger.
  - Bot tương tác Clan `/quiz` đố vui thời gian thực, BXH Clan trên Redis.
- [ ] **Milestone 3 — Sprint 3: Realtime Word Duel 1vs1 & Deep AI Tutor v2.0** *(Tuần 7 – Tuần 8)*:
  - Đấu trường 1vs1 5 vòng qua Webview nhúng Mezon kết nối SignalR GameHub.
  - Mascot LingLing phân tích sâu lỗi sai và đóng vai hội thoại.
- [ ] **Milestone 4 — Sprint 4: Đóng băng Tính năng, Kiểm thử Bảo mật NCC+ & Demo Day** *(Tuần 9 – Tuần 10)*:
  - Security Audit theo tiêu chuẩn NCC+ (Rate limiting, HMAC Webhook, SQLi, XSS).
  - Video Clip Demo 3 phút Full HD và bảo vệ đề tài trước Hội đồng Ban giám khảo.

---

*© 2026 LINGUAL Team (Team 05) — Mezon Campus Studio 2026. All rights reserved.*
