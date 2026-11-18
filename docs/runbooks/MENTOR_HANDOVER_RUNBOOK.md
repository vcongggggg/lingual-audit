# HƯỚNG DẪN KIỂM THỬ KÍN DỰ ÁN LINGUAL (MENTOR HANDOVER RUNBOOK)
**Chương trình:** Mezon Campus Studio 2026 (MCS 2026)  
**Đơn vị thực hiện:** Team 05 - Đụt Cận Trĩ  
**Chủ nhiệm đề tài / Team Lead:** Ngô Văn Công  
**Thành viên:** Nguyễn Công Minh (Backend/DB), Phan Phước Trí (Frontend/QA)  
**Mentor phụ trách:** Mai Hồng Mận (`man.maihong`)  
**Phiên bản bàn giao:** Milestone 3 - Candidate Release (Sprint 3 + Sprint 4 Ready)

---

## 1. TỔNG QUAN HỆ THỐNG & CẤU TRÚC TRIỂN KHAI

Hệ thống **LINGUAL** gồm 3 khối thành phần liên thông:
1. **Backend Modular Monolith:** ASP.NET Core 8 (`Lingual.sln`), 216 unit tests pass 100%.
2. **Webview Mini-App:** Next.js 14 App Router (`src/web-app`), 28 routes, hỗ trợ cả Desktop lẫn nhúng Mezon Tab.
3. **Mezon Bot Client:** Kết nối trực tiếp vào Clan Mezon thông qua `Mezon.Net.Sdk 1.11.0`.

---

## 2. HƯỚNG DẪN KHỞI CHẠY MÔI TRƯỜNG KIỂM THỬ

### Bước 1: Khởi động Cơ sở dữ liệu PostgreSQL & Redis
```bash
# Khởi động PostgreSQL 16 và Redis 7 (hoặc qua Docker Compose)
docker compose up -d
```
Chuỗi kết nối mặc định: `Host=localhost;Port=5432;Database=lingual_db;Username=postgres;Password=IvulKvKro_ALoEF_xN3yNT4jI8cI-HEw`

### Bước 2: Khởi chạy Backend API & Bot Gateway
```bash
cd src/backend/Lingual.Api
dotnet run
```
* **Swagger API UI:** `http://localhost:5000/` hoặc `https://localhost:5001/`
* **SignalR Realtime GameHub:** `http://localhost:5000/gamehub`

### Bước 3: Khởi chạy Webview Mini-App Next.js
```bash
cd src/web-app
npm run dev
```
* **Giao diện Webview:** `http://localhost:3000/`

---

## 3. KỊCH BẢN KIỂM THỬ 3 LUỒNG SỬ DỤNG TRỌN BỘ (END-TO-END FLOWS)

### 🎯 LUỒNG 1: HỌC TỪ VỰNG CÁ NHÂN VỚI THUẬT TOÁN SRS SM-2 & AUDIO PHÁT ÂM
1. **Truy cập:** Mở trình duyệt vào `http://localhost:3000/learn` (hoặc từ Bot gõ `/learn`).
2. **Trải nghiệm:**
   - Xem Flashcard từ vựng 3D hiển thị phiên âm IPA, từ loại, ví dụ Oxford.
   - Bấm nút **[Nghe phát âm]** hoặc phím tắt **R** để nghe giọng đọc chuẩn US bản xứ qua Web Speech API.
   - Bấm **[Chạm để xem nghĩa]** hoặc phím **Space** để lật thẻ 3D.
   - Bấm 1 trong 4 mức đánh giá: **Again (1)**, **Hard (2)**, **Good (3)**, **Easy (4)**.
3. **Kỳ vọng nghiệm thu:**
   - Hệ thống tự động tính toán khoảng cách ngày ôn tập theo công thức SM-2 chuẩn SuperMemo.
   - Khi hoàn thành bài, hiển thị popup cộng XP và cập nhật tiến độ chuỗi ngày học Streak.

---

### 🎯 LUỒNG 2: SINH HOẠT CLAN & ĐỐ VUI KHUNG GIỜ VÀNG (SCHEDULED CLAN QUIZ)
1. **Truy cập:** Vào kênh text Clan Mezon đã cài đặt Bot Lingual.
2. **Kích hoạt tự động hoặc thủ công:**
   - **Tự động:** Bot hẹn giờ chạy ngầm vào các khung 09:00, 12:00, 20:30 hàng ngày.
   - **Thử nghiệm tức thời (Admin):** Gõ `/quiz` hoặc chạy script `scripts/trigger-quiz.bat 2026-11-18_20:30`.
3. **Trải nghiệm:**
   - Bot gửi thông báo cảnh báo trước 1 phút: *"Sắp đến Thử thách Khung giờ vàng!"*.
   - Đúng giờ, Bot gửi Embed câu hỏi trắc nghiệm kèm 4 nút bấm chọn đáp án A, B, C, D và tự động ghim tin nhắn.
   - Các thành viên trong Clan bấm nút chọn đáp án.
   - Sau 30-60 giây, phiên đố tự động đóng, Bot gỡ ghim câu hỏi cũ và công bố Thẻ vinh danh người trả lời đúng nhanh nhất (+15 XP).
4. **Kỳ vọng nghiệm thu:**
   - Hoạt động đa-clan độc lập bằng khóa kết hợp `(slot_key, channel_id)`.
   - Một người học chỉ nhận thưởng XP tối đa 1 lần trong 1 khung giờ trên toàn hệ thống.

---

### 🎯 LUỒNG 3: ĐẤU TRƯỜNG ĐỐI KHÁNG 1VS1 THỜI GIAN THỰC (WORD DUEL ARENA)
1. **Khởi tạo trận đấu từ Kênh Chat Mezon:**
   - Học viên A gõ lệnh: `/duel @mentor` trong kênh chat Clan.
   - Bot tạo phòng đấu và hiển thị nút **[⚔️ Chấp nhận thách đấu]**.
   - Mentor bấm **[Chấp nhận]** -> Bot cung cấp đường dẫn mở Webview Arena kèm mã phòng `matchId`.
2. **Thi đấu Realtime qua SignalR:**
   - Cả hai mở Webview tại `http://localhost:3000/duel?matchId=<id>` (hoặc vào `http://localhost:3000/arena` bấm nút **[Đấu trường Realtime SignalR]**).
   - Server SignalR `GameHub` tự động ghép 2 người chơi vào Room, đếm ngược 3-2-1 vào trận.
   - Trận đấu diễn ra qua 5 vòng câu hỏi:
     - Mỗi câu có đồng hồ đếm ngược 10-18 giây.
     - Khi một bên chọn đáp án, thanh máu và điểm số của cả hai được cập nhật đồng bộ thời gian thực.
   - Hỗ trợ cơ chế Reconnect (< 10 giây mất mạng cho phép kết nối lại; > 10 giây xử forfeit thua an toàn).
3. **Tổng kết & Vinh danh Clan:**
   - Kết thúc 5 vòng, màn hình hiển thị người chiến thắng, bảng tổng kết điểm và nút xem giải thích chi tiết lỗi sai từ AI LingLing Mascot.
   - Bot Mezon tự động bắn Thẻ vinh danh kết quả trận đấu ra kênh chat Clan kèm lời chúc mừng.

---

## 4. BẢNG TRA CỨU CÁC LỆNH SLASH COMMANDS CỦA BOT

| Lệnh | Mô tả | Quyền hạn |
| :--- | :--- | :---: |
| `/learn` | Mở liên kết Webview học từ vựng cá nhân | Mọi học viên |
| `/daily` | Điểm danh học tập nhận XP và bảo vệ Streak hôm nay | Mọi học viên |
| `/streak` | Tra cứu chuỗi ngày học liên tục và số khiên đóng băng | Mọi học viên |
| `/top` | Xem Bảng xếp hạng Top 10 cá nhân và Top Clan | Mọi học viên |
| `/quiz` | Phát động câu hỏi đố vui từ vựng tương tác tức thì trong Clan | Thành viên / Admin |
| `/duel @user` | Thách đấu so tài từ vựng 1vs1 thời gian thực | Mọi học viên |
| `/ask <câu>` | Hỏi đáp và nhờ Mascot LingLing AI kiểm tra sửa lỗi ngữ pháp | Mọi học viên |
| `/roleplay` | Mở menu chọn kịch bản đóng vai đàm thoại tiếng Anh (Menu Dropdown) | Mọi học viên |
| `/voice` | Tạo link tham gia phòng luyện nói tiếng Anh Clan (Speaking Lounge) | Mọi học viên |
| `/grade <bài>` | Nộp đoạn văn tiếng Anh để AI chấm điểm theo 4 tiêu chí học thuật | Mọi học viên |

---

## 5. TÀI KHOẢN MẪU DÙNG THỬ (TEST CREDENTIALS)

* **Admin / Tester 1 (Chủ nhiệm):** Ngô Văn Công (`MCS03_cong.ngovan`, Mezon ID: `2100896006294999040`)
* **Tester 2 (Đấu thủ mẫu):** Nguyễn Công Minh (`MCS03_minh.nguyencong`, Mezon ID: `1967925734009737216`)
* **Tester 3 (Học viên mẫu):** Phan Phước Trí (`MCS03_tri.phanphuoc`, Mezon ID: `2041343127796584448`)
* **Chế độ kiểm thử không cần Mezon OAuth:** Thêm tham số `?token=test_token&mezon_user_id=2100896006294999040&name=MentorTest` vào URL để vào thẳng Dashboard.
