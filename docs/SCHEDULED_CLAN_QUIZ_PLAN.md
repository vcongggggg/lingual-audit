# Kế Hoạch Triển Khai: Bot Tự Động Gửi Câu Hỏi Quiz Khung Giờ Cố Định (Scheduled Clan Quiz)

## 1. Mục Tiêu & Giá Trị
- **Tự động hóa 100%**: Bot LingLing tự động chọn câu hỏi từ kho dữ liệu và phát động thử thách đố vui vào các khung giờ cố định trong ngày trên kênh Clan/Lớp học mà không cần người quản trị phải gõ lệnh `/quiz` thủ công.
- **Tạo thói quen học tập (Habit Building)**: Định hình khung giờ học tập quen thuộc cho học viên (sáng khởi động, trưa giải lao, tối ôn tập), kéo tương tác Clan tăng cao.
- **Thưởng XP server-side minh bạch**:
  - **Người trả lời đúng nhanh nhất (Winner)**: +15 XP.
  - **Người trả lời đúng tiếp theo**: +5 XP.
  - **Khuyến khích tham gia**: +2 XP cho các bạn tham gia thử sức (kể cả chưa đúng).
  - Tích lũy trực tiếp vào tiến độ Streak ngày và bảng xếp hạng Clan tuần.

---

## 2. Các Khung Giờ Đề Xuất (Múi Giờ Việt Nam UTC+7)

Mặc định đề xuất 3 khung giờ vàng phù hợp với lịch sinh hoạt của học sinh/sinh viên:
1. **Khung sáng (09:00 - Morning Boost)**: Thử thách từ vựng khởi đầu ngày mới tỉnh táo.
2. **Khung trưa (12:00 - Midday Break)**: Giải trí nhanh 60 giây sau giờ học/làm việc buổi sáng.
3. **Khung tối (20:30 - Prime Learning)**: Đố vui ôn tập buổi tối, chốt điểm Streak trước khi sang ngày mới.

> *Toàn bộ các khung giờ này sẽ được cấu hình trong `appsettings.json`, dễ dàng thêm/bớt/sửa đổi bất kỳ lúc nào mà không cần sửa code.*

---

## 3. Kiến Trúc Kỹ Thuật

```
  +-------------------------------------------------------------+
  |              ScheduledQuizBackgroundService                 |
  |  - Quét mỗi phút theo giờ Việt Nam (UTC+7)                   |
  |  - Kiểm tra Idempotency Key (chống gửi trùng lặp mốc giờ)   |
  +------------------------------+------------------------------+
                                 |
                                 v (Kích hoạt khi đến mốc giờ)
  +-------------------------------------------------------------+
  |                  ScheduledQuizService                       |
  |  - Lấy câu hỏi từ kho IClanQuizService                      |
  |  - Tạo phiên Clan Quiz thời gian 60 giây                    |
  |  - Tạo Embed chủ đề "Thử Thách Khung Giờ Vàng"             |
  |  - Đính kèm 4 nút bấm đáp án A, B, C, D                     |
  +------------------------------+------------------------------+
                                 |
                                 v
  +-------------------------------------------------------------+
  |             Kênh Mezon (# general / Clan Channel)           |
  |  - Học viên bấm nút trả lời trực tiếp trong 60s             |
  |  - Sau 60s: Tự động khóa nút, công bố người thắng và giải   |
  |    thích chi tiết, cộng XP vào Database Postgres            |
  +-------------------------------------------------------------+
```

---

## 4. Chi Tiết Các Thành Phần Cần Xây Dựng

### A. Cấu Hình Options (`ScheduledQuizOptions`)
Tạo lớp `ScheduledQuizOptions` và cấu hình trong `appsettings.json`:
```json
"ScheduledQuiz": {
  "Enabled": true,
  "TimeSlots": ["09:00", "12:00", "20:30"],
  "TargetChannelId": 0, // 0 = Tự động lấy kênh công khai mặc định của Clan
  "DurationSeconds": 60,
  "XpWinner": 15,
  "XpCorrect": 5,
  "XpParticipation": 2
}
```

### B. Background Service (`ScheduledQuizBackgroundService`)
- Kế thừa `Microsoft.Extensions.Hosting.BackgroundService` (tương tự như `SrsReminderBackgroundService`).
- Chạy nền độc lập, kiểm tra chu kỳ `TimeSpan.FromMinutes(1)`.
- Chuyển đổi sang `DateTime.UtcNow.AddHours(7)` (Giờ Việt Nam).
- **Cơ chế Idempotency chống gửi lặp**: Lưu `_lastTriggeredSlotKey = $"{now:yyyy-MM-dd}_{slot}"`. Dù bot restart hoặc chạy lâu cũng chỉ kích hoạt đúng 1 lần cho mỗi khung giờ trong ngày.

### C. Logic Phát Động Thử Thách (`IScheduledQuizService`)
- Kiểm tra kênh mục tiêu (lấy từ cấu hình hoặc kênh chat mặc định của Clan).
- Kiểm tra xem kênh có đang bận phiên Quiz nào khác không (`HasActiveSessionAsync`).
- Gọi `IClanQuizService.StartSessionAsync(channelId, startedByMezonUserId: null, durationSeconds: 60, isPractice: false)`.
- Gửi tin nhắn Embed đặc biệt với banner:
  - **Tiêu đề**: `⏰ THỬ THÁCH KHUNG GIỜ VÀNG (09:00) — ĐỐ VUI TỪ VỰNG CLAN`
  - **Mô tả**: `⚡ Nhanh tay chọn đáp án chính xác trong 60 giây để ghi điểm cho Clan và nhận thưởng XP!`
  - **4 nút bấm**: A, B, C, D (hoạt động công khai cho cả nhóm cùng thi đua).
- Đặt timer 60 giây: Khi hết giờ, gọi `ConcludeSessionAsync` tổng kết bảng vinh danh và vô hiệu hóa nút bấm.

### D. Lệnh Hỗ Trợ Quản Trị & Live Test Tức Thời
- Bổ sung lệnh `/quiz trigger` hoặc `/quiz test`: Cho phép kích hoạt thử nghiệm một phiên khung giờ vàng ngay lập tức mà không cần phải chờ đến 09:00 hay 20:30.

---

## 5. Kế Hoạch Triển Khai Từng Bước

1. **Bước 1**: Tạo file `ScheduledQuizOptions.cs` trong `Lingual.Modules.MezonBot`.
2. **Bước 2**: Tạo Interface `IScheduledQuizService` và Service `ScheduledQuizService` xử lý logic chọn câu hỏi, đóng gói Embed khung giờ vàng và phát tin nhắn.
3. **Bước 3**: Tạo `ScheduledQuizBackgroundService` quản lý lịch trình tự động theo giờ Việt Nam.
4. **Bước 4**: Đăng ký Service và HostedService vào `Program.cs` / `MezonBotModule.cs`.
5. **Bước 5**: Bổ sung nhánh lệnh `/quiz trigger` trong `MezonBotWorker.cs` để hỗ trợ anh Công live test ngay.
6. **Bước 6**: Viết Unit Tests mới xác thực lịch trình, idempotency và logic phát tin nhắn (đảm bảo 170/170 test cũ tiếp tục pass 100%).
7. **Bước 7**: Build toàn bộ solution, commit và push lên nhánh `cong/feat/fix-m2-3-f1-f2-f3` tại repo `audit`.
8. **Bước 8**: Khởi động daemon `Lingual.Api` và hướng dẫn anh Công gõ lệnh test thử nghiệm trực tiếp trên Mezon.
