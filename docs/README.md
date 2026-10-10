# 📚 LINGUAL — HỆ THỐNG TÀI LIỆU DỰ ÁN (MEZON CAMPUS STUDIO 2026)

> **Dự án:** LINGUAL (LinguaMezon) — Nền tảng học ngoại ngữ cộng đồng trên Mezon Platform  
> **Chương trình:** Mezon Campus Studio 2026 (NCC+ & Mezon Platform)  
> **Đội ngũ (Team 05 — Đụt Cận Trĩ):** Ngô Văn Công (Lead), Nguyễn Công Minh (DB/Backend), Phan Phước Trí (Frontend/QA)  
> **Mentor:** Mai Hồng Mận  

---

## 🎯 1. HỒ SƠ NỘP BÀI CHÍNH THỨC (MILESTONE 1 - HẠN 12/10/2026)

Tất cả hồ sơ sản phẩm đã được phân loại chuẩn hóa vào thư mục [`product/`](product/):

* 📘 **Tài liệu Yêu cầu Sản phẩm & Đặc tả Nghiệp vụ (PRD / SRS):**
  * Bản Word chuẩn A4: [`product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx`](product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx)
  * Bản Markdown v1.1: [`product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.md`](product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.md)
* ⏱️ **Kế hoạch Sprint 10 Tuần (Sprint Roadmap & Backlog):**
  * Bản Word chuẩn A4: [`product/SPRINT_PLAN_LINGUAL_MCS2026.docx`](product/SPRINT_PLAN_LINGUAL_MCS2026.docx)
  * Bản Markdown v1.1: [`product/SPRINT_PLAN_LINGUAL_MCS2026.md`](product/SPRINT_PLAN_LINGUAL_MCS2026.md)
  * Bản tóm tắt lộ trình: [`product/SPRINT_ROADMAP.md`](product/SPRINT_ROADMAP.md)
* 🚀 **Gói Tác chiến & Kế hoạch Chi tiết Từng Ngày (04/10 → 12/10):**
  * Hướng dẫn chi tiết, checklist Definition of Done: [`product/LINGUAL_MILESTONE1_PACK.md`](product/LINGUAL_MILESTONE1_PACK.md)

---

## 📂 2. CẤU TRÚC TOÀN BỘ CÁC THƯ MỤC TÀI LIỆU

Hệ thống tài liệu được đồng bộ đầy đủ theo cấu trúc chuẩn của nhóm:

| Thư mục | Mục đích & Nội dung | Tài liệu chính |
| :--- | :--- | :--- |
| **`product/`** | Toàn bộ hồ sơ PRD v1.1, Kế hoạch Sprint 10 tuần, Lộ trình tác chiến Milestone 1 | [`PRD.docx`](product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx), [`SPRINT_PLAN.docx`](product/SPRINT_PLAN_LINGUAL_MCS2026.docx), [`MILESTONE1_PACK.md`](product/LINGUAL_MILESTONE1_PACK.md) |
| **`database/`** | Thiết kế CSDL PostgreSQL 16 (9 modules SQL & DBML) | [`sql/`](database/sql/) (9 file DDL + `00_init_all.sql`), [`lingual_full_schema.dbml`](database/lingual_full_schema.dbml) (22 bảng Core MVP chuẩn) |
| **`architecture/`** | Thiết kế kiến trúc .NET 8 Modular Monolith, SignalR GameHub, Single-Clan Bot | [`ARCHITECTURE_DESIGN.md`](architecture/ARCHITECTURE_DESIGN.md), `system-design.md`, `workflow.md` |
| **`experience/`** | Đặc tả trải nghiệm người dùng & Ma trận phân quyền RBAC | [`role-permission.md`](experience/role-permission.md), [`role/learner.md`](experience/role/learner.md), [`role/clan-moderator.md`](experience/role/clan-moderator.md) |
| **`templates/`** | Các biểu mẫu báo cáo daily standup, biểu mẫu PRD | [`DAILY_STANDUP_TEMPLATE.md`](templates/DAILY_STANDUP_TEMPLATE.md), [`PRD_TEMPLATE.md`](templates/PRD_TEMPLATE.md) |
| **`reference/`** | Tài liệu tham chiếu kỹ thuật, API, dữ liệu, sự kiện | `api.md`, `data-model.md`, `events.md`, `mezon-channel-app.md` |
| **`rules/`** | Bộ quy tắc nghiệp vụ, tính điểm, gamification | Quy tắc XP Ledger, Streak, Luật đấu Word Duel |
| **`runbooks/`** | Hướng dẫn vận hành, kiểm thử, triển khai | Hướng dẫn chạy Docker, Seed dữ liệu, Deploy VPS |
| **`assets/`** | Hình ảnh, biểu đồ, sơ đồ kiến trúc | [`assets/diagram_sprint_timeline.png`](assets/diagram_sprint_timeline.png) |

---

## 👥 3. PHÂN VAI & TÀI LIỆU CẦN XEM HÀNG NGÀY

| Thành viên | Vai trò phụ trách | Tài liệu cần mở đọc hàng ngày |
| :--- | :--- | :--- |
| **Công (Lead)** | Tích hợp Mezon Bot, Realtime SignalR, Chatbot AI LingLing | [`product/LINGUAL_MILESTONE1_PACK.md`](product/LINGUAL_MILESTONE1_PACK.md)<br>[`architecture/ARCHITECTURE_DESIGN.md`](architecture/ARCHITECTURE_DESIGN.md) |
| **Minh (DB/BE)** | Thiết kế ERD CSDL, EF Core Migrations, Thuật toán SRS SM-2 | [`database/lingual_full_schema.dbml`](database/lingual_full_schema.dbml)<br>[`database/sql/`](database/sql/) |
| **Trí (FE/QA)** | Giao diện Next.js 14 Webview, 500 từ vựng A1-A2, Test Plan | [`experience/role/learner.md`](experience/role/learner.md)<br>Template CSV từ vựng & Test cases |
