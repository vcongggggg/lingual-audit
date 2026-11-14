# Kế Hoạch Triển Khai: Bot Tự Động Gửi Câu Hỏi Quiz Khung Giờ Cố Định (Scheduled Clan Quiz)

## 1. Mục Tiêu & Giá Trị
- **Tự động hóa 100%**: Bot LingLing tự động chọn câu hỏi từ kho dữ liệu (>= 30 câu CEFR) và phát động thử thách đố vui vào các khung giờ cố định trong ngày trên kênh Clan/Lớp học mà không cần người quản trị phải gõ lệnh `/quiz` thủ công.
- **Tạo thói quen học tập (Habit Building)**: Định hình khung giờ học tập quen thuộc cho học viên (sáng khởi động, trưa giải lao, tối ôn tập), kéo tương tác Clan tăng cao.
- **Thưởng XP server-side minh bạch**:
  - **Người trả lời đúng nhanh nhất (Winner)**: +15 XP.
  - **Người trả lời đúng tiếp theo**: +5 XP.
  - **Khuyến khích tham gia**: 0 XP (chỉ người trả lời đúng mới nhận XP, không cấp XP tham gia để chống spam bot).
  - **Lưu ý về Streak**: Quiz XP được ghi nhận vào tổng XP và bảng xếp hạng Clan tuần. Quiz XP KHÔNG ảnh hưởng hay tích lũy vào Streak ngày (Streak chỉ được tính qua điểm danh `/daily`).
  - **Quy tắc liên Clan (Cross-Clan Single Reward)**: Mỗi học viên chỉ được nhận XP tối đa 1 lần cho mỗi khung giờ trong ngày (xác định qua `slot_key`). Nếu học viên đã nhận XP tại Clan A, khi tham gia trả lời đúng tại Clan B trong cùng khung giờ sẽ nhận 0 XP kèm thông báo: *"Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP"*.

---

## 2. Các Khung Giờ Đề Xuất (Múi Giờ Việt Nam UTC+7 - Asia/Ho_Chi_Minh)

Mặc định cấu hình 3 khung giờ vàng phù hợp với lịch sinh hoạt của học sinh/sinh viên:
1. **Khung sáng (09:00 - Morning Boost)**: Thử thách từ vựng khởi đầu ngày mới tỉnh táo.
2. **Khung trưa (12:00 - Midday Break)**: Giải trí nhanh 60 giây sau giờ học/làm việc buổi sáng.
3. **Khung tối (20:30 - Prime Learning)**: Đố vui ôn tập buổi tối trước khi sang ngày mới.

> *Toàn bộ các khung giờ này được cấu hình trong `appsettings.json`, dễ dàng thêm/bớt/sửa đổi bất kỳ lúc nào mà không cần sửa code.*

---

## 3. Kiến Trúc Kỹ Thuật

```
  +-------------------------------------------------------------+
  |              ScheduledQuizBackgroundService                 |
  |  - Quét mỗi phút theo TimeZone "Asia/Ho_Chi_Minh"           |
  |  - Báo trước 1 phút: [slot - 1m, slot)                      |
  |  - Phát động quiz: [slot, slot + 10m) (hỗ trợ catch-up)     |
  |  - Idempotency DB qua bảng scheduled_quiz_runs              |
  +------------------------------+------------------------------+
                                 |
                                 v (Kích hoạt khi đến mốc giờ)
  +-------------------------------------------------------------+
  |                  ScheduledQuizService                       |
  |  - Gỡ ghim tin nhắn quiz cũ, ghim tin nhắn quiz mới        |
  |  - Lấy câu hỏi từ kho IClanQuizService (>= 30 câu CEFR)     |
  |  - Lọc câu hỏi chưa hỏi trong 7 ngày gần nhất               |
  |  - Tạo phiên Clan Quiz thời gian 60 giây                    |
  |  - Embed "Thử Thách Khung Giờ Vàng" kèm 4 nút đáp án        |
  +------------------------------+------------------------------+
                                 |
                                 v
  +-------------------------------------------------------------+
  |             Kênh Mezon (# general / Clan Channel)           |
  |  - Học viên bấm nút trả lời trực tiếp trong 60s             |
  |  - Sau 60s: Tự động khóa nút, công bố người thắng và giải   |
  |    thích chi tiết, cộng XP vào Database Postgres            |
  |  - Ghi nhận lịch sử vào scheduled_quiz_participations       |
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
  "TargetChannelId": 1819651515982843904,
  "DurationSeconds": 60,
  "XpWinner": 15,
  "XpCorrect": 5,
  "XpParticipation": 0,
  "TimeZoneId": "Asia/Ho_Chi_Minh",
  "AdminUserIds": ["1794149317681", "1793710923342"]
}
```
*Fail-fast validation*: Ném `InvalidOperationException` nếu `Enabled == true` nhưng `TargetChannelId <= 0`.

### B. Database Migration & Entity Models
1. **`ScheduledQuizRun`** (`scheduled_quiz_runs`):
   - `SlotKey` (PK, string, format: `yyyy-MM-dd_HH:mm`).
   - `ChannelId` (long), `SessionId` (Guid?), `PinnedMessageId` (long?), `WarnedAt` (DateTime?), `PostedAt` (DateTime?), `CreatedAt` (DateTime).
2. **`ScheduledQuizParticipation`** (`scheduled_quiz_participations`):
   - `Id` (Guid, PK).
   - `SlotKey` (string), `UserId` (Guid), `MezonUserId` (string), `ClanId` (long), `ChannelId` (long), `SessionId` (Guid), `IsCorrect` (bool), `XpAwarded` (int), `CreatedAt` (DateTime).
   - **Ràng buộc UNIQUE**: `(SlotKey, UserId)` ngăn chặn nhận thưởng nhiều lần trên nhiều clan.
3. **Cập nhật Entity cũ**:
   - `QuizQuestion`: Bổ sung `DateTime? LastAskedAt` để thuật toán chọn câu hỏi ưu tiên câu chưa xuất hiện trong 7 ngày gần nhất.
   - `ClanQuizSession`: Bổ sung `string? SlotKey`.

### C. Background Service (`ScheduledQuizBackgroundService`)
- Kế thừa `Microsoft.Extensions.Hosting.BackgroundService`.
- Chạy nền độc lập, kiểm tra chu kỳ `TimeSpan.FromMinutes(1)`.
- Xác định thời gian chuẩn qua `TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)` (CẤM dùng `AddHours(7)` thô).
- **Cơ chế báo trước 1 phút**: Quét mốc `[slot - 1m, slot)` và gọi `PostWarningAsync` thông báo sắp có thử thách.
- **Cơ chế Idempotency DB**: Kiểm tra trạng thái trong bảng `scheduled_quiz_runs`. Dù bot restart hoặc có trễ mạng (catch-up trong vòng 10 phút `[slot, slot + 10m)`), mỗi slot chỉ được phát động đúng 1 lần.
- Dọn dẹp phiên treo (`CleanUpStaleSessionsAsync`) khi khởi động.

### D. Logic Phát Động Thử Thách & Quản Trị Ghim Tin Nhắn (`IScheduledQuizService`)
- Gỡ ghim tin nhắn quiz khung giờ cũ (`DeletePinMessageAsync`) để tránh làm loãng tin nhắn ghim kênh.
- Phát động phiên quiz 60s, ghim tin nhắn quiz mới (`CreatePinMessageAsync`).
- Xử lý nộp bài qua `IClanQuizService.SubmitAnswerAsync`:
  - Kiểm tra `scheduled_quiz_participations` cho cặp `(slotKey, userId)`.
  - Nếu đã tham gia ở clan khác: `xpAwarded = 0`, thông báo *"Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP"*.
  - Nếu chưa tham gia: Ghi nhận XP hợp lệ (Winner: +15, Correct: +5) vào `xp_ledger` với idempotency key `scheduled_quiz_{slotKey}_{userId}`.
- Kết thúc phiên 60s: Tổng kết vinh danh công khai, hiển thị đáp án và giải thích chi tiết, gỡ ghim nếu cần.

### E. Lệnh Quản Trị (`/quiz trigger`)
- Bổ sung lệnh `/quiz trigger` trong `MezonBotWorker.cs`:
  - Kiểm tra phân quyền: Chỉ các tài khoản thuộc `AdminUserIds` mới được phép kích hoạt.
  - Cooldown: Giới hạn tối thiểu 60 giây giữa các lần kích hoạt trên cùng một kênh.
  - Tối đa 1 active session đang chạy trên một kênh.
  - Hỗ trợ tham số slotKey tùy chọn hoặc tự sinh theo thời điểm hiện tại.

---

## 5. Quy Trình Nghiệm Thu & Kiểm Thử
1. **Unit Tests Toàn Diện**:
   - Schedule matching theo múi giờ Việt Nam.
   - Database Idempotency (chống post lặp).
   - Cross-clan XP rules (chỉ 1 clan nhận XP, clan thứ 2 nhận 0 XP).
   - Báo trước 1 phút và ghim/gỡ ghim tin nhắn.
   - Fail-fast khi cấu hình sai TargetChannelId.
   - Phân quyền lệnh `/quiz trigger`.
2. **Build Verification**:
   - `dotnet build` sạch 0 lỗi, 0 cảnh báo.
   - `dotnet test` bảo đảm toàn bộ test bot và solution pass 100%.
3. **Audit & Giao Nhận**:
   - Push commit lên nhánh `cong/feat/fix-m2-3-f1-f2-f3` tại remote `audit`.
   - TUYỆT ĐỐI KHÔNG khởi động `Lingual.Api` và KHÔNG live test trên Mezon trước khi Muse audit xong.
   - Xuất file nghiệm thu `task_done` vào `C:\Mod\muse-mailbox\inbox`.
