# Tài Liệu UI Mockup & Quy Chuẩn Thiết Kế Lingual (Được trích xuất từ LinguaFlow)

Tài liệu này tổng hợp toàn bộ Mockup giao diện và Ngôn ngữ thiết kế (Design System) từ dự án **LinguaFlow** chuyển giao sang **Mezon Campus Studio (LINGUAL)**.

---

## 1. Bản Mockup Tương Tác Trực Tiếp (Interactive Prototype)
- **File:** [mockup-lingual-giay-linhvat.html](file:///c:/Study/HocKy6/MezonCampusStudio/docs/ui-mockup/mockup-lingual-giay-linhvat.html)
- **Phong cách:** **Retro-Paper & Neobrutalism** kết hợp Linh vật LingLing.
- **Bảng màu chủ đạo:**
  - Nền giấy: `--paper: #FAF5EA`, `--paper-2: #F1E9D6`, thẻ card: `--card: #FFFDF6`
  - Màu mực viền: `--ink: #211A13`, `--ink-soft: #6B5F4E`, nét viền: `2px solid var(--ink)`
  - Màu điểm nhấn: `--red: #D64027`, `--teal: #0F766E`, `--amber: #F2A93B`
- **Font chữ:** `Be Vietnam Pro` (400, 600, 800, 900)
- **Hiệu ứng có sẵn:**
  - Card 3D Flip (Lật thẻ trước / sau có Spaced Repetition SRS rating).
  - Micro-interactions nút bấm đổ bóng mực (`box-shadow: 4px 4px 0 var(--ink)`).
  - Layout chia đôi màn hình học tập và tương tác Mascot.

---

## 2. Quy Chuẩn Ngôn Ngữ Thiết Kế (Design System & UI Rules)
- **[LINGUAFLOW_DESIGN_SYSTEM.md](file:///c:/Study/HocKy6/MezonCampusStudio/docs/ui-mockup/LINGUAFLOW_DESIGN_SYSTEM.md):**
  - Triết lý Bespoke Cyber-Academic Aesthetic (cho Dark Mode) & Paper Neobrutalism (cho Light Mode).
  - 5 trạng thái giao diện bắt buộc (`Default`, `Loading/Skeleton`, `Empty State`, `Error/Retry`, `Success/Active`).
  - Hệ thống Token màu, Spacing và Glassmorphism.
- **[LINGUAFLOW_UI_RULES.md](file:///c:/Study/HocKy6/MezonCampusStudio/docs/ui-mockup/LINGUAFLOW_UI_RULES.md):**
  - Quy tắc phân bổ component, tỉ lệ font, padding/margin responsive cho mobile/desktop.

---

## 3. Kho Ảnh Chụp 62 Màn Hình Giao Diện Thực Tế (Screenshots Catalog)
Toàn bộ ảnh chụp giao diện độ phân giải cao được lưu trữ tại:
`C:\Study\HocKy6\LinguaFlow\screenshot\`

### Danh mục màn hình tiêu biểu:
1. **Tổng quan & Đăng nhập:**
   - `01_landing_page_vi.png` / `02_landing_page_en.png`: Trang giới thiệu sản phẩm.
   - `03_dashboard_roadmap.png`: Bản đồ lộ trình học tập cá nhân hóa.
   - `04_login_page.png` / `05_register_page.png`: Xác thực người dùng.
2. **Khu vực Từ vựng & Spaced Repetition (SRS):**
   - `08_srs_flashcards.png`: Màn hình học Flashcard theo thuật toán SM-2 lật thẻ 3D.
   - `14_vocabulary_lab_hub.png` / `15_vocabulary_practice.png` / `16_vocabulary_test.png`: Trung tâm luyện từ vựng.
3. **Khu vực Gamification & Đấu trường:**
   - `10_arcade_games.png`: Các minigame ôn tập từ vựng thời gian thực.
   - `46_community_leaderboard.png`: Bảng xếp hạng XP tuần / clan thời gian thực.
   - `50_community_achievements_wall.png`: Tường thành tựu và huy hiệu.
4. **Khu vực Trợ lý AI (LingLing Tutor):**
   - `57_ai_tutor_chat_workspace.png`: Khung chat tương tác với AI Tutor LingLing.
   - `58_ai_tutor_dashboard_analytics.png`: Phân tích tiến độ học tập bằng AI.
   - `59_ai_tutor_adaptive_plan.png`: Lộ trình thích ứng tự động điều chỉnh.
5. **Khu vực Kỹ năng & Luyện thi:**
   - `11_listening_lab_hub.png` -> `13_listening_shadowing.png`: Luyện nghe chính tả và nhại giọng.
   - `19_writing_lab_hub.png` -> `22_writing_free_journal.png`: Luyện viết có chấm chữa ngữ pháp.
   - `35_exam_practice_hub.png` -> `44_ielts_full_mock_test.png`: Bộ đề thi thử TOEIC / IELTS.
