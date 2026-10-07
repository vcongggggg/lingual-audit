# 🚀 LINGUAL (LINGUAMEZON) — MEZON CAMPUS STUDIO 2026

[![Mezon Campus](https://img.shields.io/badge/Program-Mezon_Campus_Studio_2026-6366f1?style=for-the-badge)](https://campus.mezon.vn)
[![Organizer](https://img.shields.io/badge/Organizer-NCC%2B%20%7C%20Mezon-06b6d4?style=for-the-badge)](https://mezon.ai)
[![Project](https://img.shields.io/badge/Project-Lingual_Language_Learning-10b981?style=for-the-badge)](#)
[![Status](https://img.shields.io/badge/Status-Phase_1:_PRD_&_Ideation-f59e0b?style=for-the-badge)](#)

> **Dự án:** LINGUAL — Social & Gamified Language Learning Platform  
> **Nhóm trưởng / Project Lead:** `cong.ngovan` (Ngô Văn Công) — Khoa CNTT, Đại học Bách Khoa – ĐH Đà Nẵng  
> **Tech Stack chính thức (Mentor chốt):**  
> - **Backend:** **ASP.NET Core (Kiến trúc Modular Monolith)**  
> - **Frontend / Mini-App:** **Next.js 14**  
> **Workspace:** `C:\Study\HocKy6\MezonCampusStudio`  
> **Git Repository:** Đã kết nối với repo chính thức `mezon-campus-studio-06-2026-ut-can-tri` (chỉ fetch/pull, không push khi chưa có lệnh)  
> **Phương pháp quản lý:** Agile/Scrum Weekly Sprint, Daily Standup & Security Audit  

---

## 📂 1. CẤU TRÚC THƯ MỤC DỰ ÁN (PROJECT REPOSITORY LAYOUT)

```text
MezonCampusStudio/
├── docker-compose.yml         # Môi trường chạy local: PostgreSQL 16 & Redis 7
├── .env.example               # Mẫu biến môi trường (Mezon credentials, DB, Redis)
├── .gitignore                 # Cấu hình bỏ qua mã rác, secret, bin/obj và node_modules
├── README.md                  # Tài liệu tổng quan dự án
├── docs/                      # Tài liệu kỹ thuật & quy trình Agile
│   ├── PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.md # [CHÍNH THỨC] Tài liệu yêu cầu sản phẩm (PRD/SRS)
│   ├── PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx # Bản Word chuẩn in ấn/nộp bài (có sơ đồ minh họa)
│   ├── LINGUAL_PROJECT_PLAN_MCS2026.md          # Kế hoạch điều phối & phân công thành viên
│   └── SPRINT_ROADMAP.md      # Lộ trình 10 tuần chi tiết (Milestone 1 -> 4)
├── src/                       # Mã nguồn hệ thống
│   ├── backend/               # ASP.NET Core 8 Modular Monolith Solution
│   │   ├── Lingual.sln        # Visual Studio / .NET Solution
│   │   ├── Lingual.Api/       # Web API Host, Swagger, SignalR GameHub, HealthChecks
│   │   ├── Lingual.Shared/    # BaseEntity, ApiResponse, Shared Kernel
│   │   ├── Lingual.Modules.Identity/     # Mezon SSO OAuth2, JWT Bearer
│   │   ├── Lingual.Modules.Learning/     # Khóa học, bài học, từ vựng, SRS SM-2 Engine
│   │   ├── Lingual.Modules.Gamification/ # XP, Streak, BXH, Word Duel SignalR Hub
│   │   ├── Lingual.Modules.MezonBot/     # Tích hợp Bot Mezon, Event handlers
│   │   └── Lingual.Modules.AITutor/      # Mascot LingLing, Gemini LLM API
│   │
│   └── web-app/               # Next.js 14 App Router (Tailwind CSS, Lucide Icons, SignalR)
│       ├── src/app/           # Layouts, Routes
│       ├── src/components/    # UI Components
│       └── package.json       # Dependencies
├── scripts/                   # Công cụ tự động hóa & báo cáo
│   ├── verify_env.py          # Kiểm tra môi trường phát triển
│   ├── patch_docx_diagrams.py # Vá sơ đồ đồ họa vào file Word in-place
│   └── daily_report.py        # Tự động xuất Daily Standup & gửi thông báo
└── tests/                     # Bộ kiểm thử Unit & Integration Tests
```

---

## 🛠️ 2. BỘ CÔNG NGHỆ CHÍNH ĐÃ PHÊ DUYỆT (OFFICIAL TECH STACK)

* **Backend Lõi:** **ASP.NET Core 8 (Kiến trúc Modular Monolith)**:
  - **Modules:** `Modules.Identity`, `Modules.Learning` (SRS SM-2), `Modules.Gamification` (SignalR Realtime Duels), `Modules.MezonBot`, `Modules.AITutor`.
  - **ORM & Data:** Entity Framework Core (EF Core 8) + LINQ Type-safe.
  - **Realtime Game:** ASP.NET Core SignalR (1vs1 Word Duel & Live Clan Quiz).
* **Frontend / Channel Mini-App:** **Next.js 14 (App Router)**, React 19, TailwindCSS v4, Lucide Icons.
* **Mezon Platform:** `mezon-sdk` (TypeScript Gateway), Mezon Gateway WebSockets, Webhooks, Mezon Desktop MCP.
* **Cơ sở dữ liệu & Cache:** PostgreSQL 16 (RDBMS), Redis 7 (Sorted Sets cho Realtime Leaderboards & Sliding-window Rate Limiting).
* **Trí tuệ nhân tạo (AI):** Google Gemini 1.5 Flash (sửa lỗi ngữ pháp & đóng vai hội thoại Mascot LingLing).
* **Bảo mật & Audit (Chuẩn NCC+):** ASP.NET Core Built-in Rate Limiting (`SlidingWindow`), JWT Auth, Webhook Signature Verification, FluentValidation.
* **DevOps & QA:** Docker / Docker Compose, GitHub Actions CI/CD, xUnit (.NET).

---

## ⚡ 3. HƯỚNG DẪN KHỞI CHẠY NHANH (QUICK START)

### 3.1 Khởi động Hạ tầng Database & Cache (Docker):
```bash
docker compose up -d
```
*(Khởi chạy PostgreSQL 16 tại cổng `5432` và Redis 7 tại cổng `6379`).*

### 3.2 Khởi động Backend (ASP.NET Core 8):
```bash
cd src/backend
dotnet run --project Lingual.Api
```
- Swagger API Docs: [http://localhost:5000](http://localhost:5000) (hoặc `http://localhost:5239`)
- SignalR GameHub: `/hubs/game`
- Health Check: `/health`

### 3.3 Khởi động Frontend (Next.js 14):
```bash
cd src/web-app
npm run dev
```
- Giao diện người dùng: [http://localhost:3000](http://localhost:3000)

### 3.4 Báo cáo tiến độ Daily:
```bash
# Xem báo cáo Standup trên màn hình
python scripts/daily_report.py

# Gửi trực tiếp về Telegram cá nhân
python scripts/daily_report.py --telegram
```

---

## 📅 4. TIẾN ĐỘ 3 GIAI ĐOẠN (ROADMAP OVERVIEW)
- [x] **Phase 1: Kickoff & Thiết lập nền tảng dự án** (29/06 – 15/07/2026).
- [ ] **Phase 2: Sprint Weekly & Thực thi tính năng** (15/07 – 09/09/2026).
- [ ] **Phase 3: Security Audit, Demo Day & Public Launch** (09/09 – 23/09/2026).

---

*Dự án đã sẵn sàng 100% để tiếp nhận ý tưởng sản phẩm cụ thể và tiến hành code các module theo chỉ đạo của anh Văn Công!*
