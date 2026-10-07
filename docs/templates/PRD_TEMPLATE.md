# 📋 PRODUCT REQUIREMENTS DOCUMENT (PRD)
## DỰ ÁN: [TÊN DỰ ÁN CỦA BẠN] — MEZON CAMPUS STUDIO 2026

> **Product Owner / Lead Developer:** Ngô Văn Công  
> **Mentor:** [Tên Mentor NCC+ / Mezon]  
> **Version:** 1.0.0 | **Ngày cập nhật:** 30/09/2026  
> **Trạng thái:** DRAFT / UNDER_REVIEW / APPROVED  

---

### 1. 🎯 TỔNG QUAN & TẦM NHÌN SẢN PHẨM (PRODUCT OVERVIEW)
#### 1.1 Vấn đề thực tế (Problem Statement)
* *Mô tả nỗi đau (pain point) của người dùng hoặc các Clan trên nền tảng Mezon hiện nay:*
  - ...
  - ...

#### 1.2 Giải pháp & Giá trị mang lại (Proposed Solution & Value Proposition)
* *Sản phẩm giải quyết nỗi đau trên như thế nào?*
  - ...

#### 1.3 Đối tượng người dùng mục tiêu (Target Persona)
* **Nhóm 1:** Thành viên Clan thông thường (Gamers, sinh viên, cộng đồng sở thích...).
* **Nhóm 2:** Clan Admins / Moderators (Người quản trị cộng đồng cần công cụ quản lý tự động).
* **Nhóm 3:** Khách vãng lai / Người dùng mới tham gia vào hệ sinh thái.

---

### 2. 🚀 PHẠM VI TÍNH NĂNG (FEATURE SCOPE)

#### 2.1 Tính năng cốt lõi (Core / MVP - Minimum Viable Product)
- [ ] **Feature 1:** [Tên tính năng MVP 1 - Ví dụ: Onboarding & Verify thành viên tự động]
  - *Mô tả:* ...
  - *Tiêu chí nghiệm thu (Acceptance Criteria):* ...
- [ ] **Feature 2:** [Tên tính năng MVP 2 - Ví dụ: Interactive Clan Bot / Slash Commands]
  - *Mô tả:* ...
  - *Tiêu chí nghiệm thu:* ...
- [ ] **Feature 3:** [Tên tính năng MVP 3 - Ví dụ: Hệ thống tích điểm, Mini-game hoặc AI Assistant]
  - *Mô tả:* ...
  - *Tiêu chí nghiệm thu:* ...

#### 2.2 Tính năng nâng cao (Post-MVP / Nice-to-have)
- [ ] **Feature 4:** Dashboard Web Analytics chi tiết cho Clan Master.
- [ ] **Feature 5:** Tích hợp đa nền tảng (Webhooks, Notification Sync về Telegram/Discord).

---

### 3. 📐 THIẾT KẾ TRẢI NGHIỆM & USER FLOW (UX & USER STORIES)
1. **User Story 1:** *"Là một thành viên Clan, tôi muốn gõ lệnh `/help` để xem nhanh danh sách tính năng hỗ trợ, giúp tôi làm quen ngay lập tức."*
2. **User Story 2:** *"Là một Quản trị viên, tôi muốn nhận thông báo khi có thành viên vi phạm quy tắc Clan để kịp thời xử lý."*
3. **User Story 3:** ...

---

### 4. ⚙️ YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)
* **Hiệu năng (Performance):** Thời gian phản hồi API/Bot message <= 500ms cho 95% request.
* **Độ sẵn sàng (Availability):** 99.9% uptime trong giờ cao điểm hoạt động Clan.
* **Bảo mật (Security - Tiêu chuẩn gắt gao Mezon/NCC+):**
  - Không hardcode API key / Token trong mã nguồn.
  - Chống SQL Injection, XSS, CSRF và Rate Limiting chống spam tin nhắn.
  - Phân quyền RBAC (Role-Based Access Control) chặt chẽ giữa User và Admin.
* **Khả năng mở rộng (Scalability):** Dễ dàng mở rộng cho nhiều Clan (Multi-tenancy).

---

### 5. 🗓️ KẾ HOẠCH MILESTONE & GIAO HÀNG (DELIVERY TIMELINE)
* **Tuần 1 - 2 (Phase 1):** Chốt PRD, thiết kế Architecture, duyệt với Mentor.
* **Tuần 3 - 6 (Phase 2 - Sprint 1 & 2):** Xây dựng Core Engine & Database Model.
* **Tuần 7 - 8 (Phase 2 - Sprint 3 & 4):** Tích hợp Mezon SDK, kiểm thử tương tác thực tế trong Clan test.
* **Tuần 9 (Phase 2 - Security Audit):** Rà soát mã nguồn, vá lỗ hổng bảo mật theo tiêu chuẩn NCC+.
* **Tuần 10 (Phase 3 - Launching & Demo Day):** Triển khai Production, chuẩn bị slide và demo trước hội đồng.

---

### 6. 📊 CHỈ SỐ ĐO LƯỜNG THÀNH CÔNG (SUCCESS METRICS / KPIS)
* Số lượng Clan cài đặt thử nghiệm: `>= [X]` Clan.
* Số lượt tương tác tin nhắn / lệnh mỗi ngày: `>= [Y]` interactions/day.
* Đánh giá mức độ hài lòng từ Mentor & Ban giám khảo NCC+: `>= 8.5/10`.
