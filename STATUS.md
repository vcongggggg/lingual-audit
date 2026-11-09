# 📊 BÁO CÁO AUDIT ĐỘC LẬP SPRINT 3 — LINGUAL (MCS 2026)
> **Dự án:** LINGUAL — Nền tảng Học Ngoại Ngữ Xã Hội Tích Hợp AI & Mezon Platform  
> **Nhánh Audit:** `cong/feat/sprint3-word-duel-and-deep-ai`  
> **Commit Thẩm Định:** `84e43a8` (Mới nhất)  
> **Ngày thực hiện:** 09/11/2026  
> **Kiểm toán viên:** Senior Security Architect & Independent Code Reviewer  
> **Tình trạng nghiệm thu:** **ĐẠT CHUẨN XUẤT SẮC (9.4 / 10)**

---

## 1. TỔNG QUAN KẾT QUẢ AUDIT 4 TRỌNG TÂM

| Trọng tâm Thẩm định | Đánh giá | Trạng thái Bằng chứng | Điểm thành phần |
| :--- | :---: | :---: | :---: |
| **1. Đấu trường từ vựng 1v1 & SignalR GameHub** | Xuất sắc | `[VERIFIED]` Chạy thực tế & Static code | **9.5 / 10** |
| **2. Deep AI Mistake Tutor v2.0** | Rất tốt | `[VERIFIED]` Unit test & Fallback | **9.4 / 10** |
| **3. Mezon Bot Ephemeral Cards & Anti-Spam** | Hoàn hảo | `[VERIFIED]` Multi-thread simulation | **9.7 / 10** |
| **4. Kiểm thử Unit Test & Bảo mật OWASP** | 100% Pass | `[VERIFIED]` 107/107 Unit Tests Green | **9.2 / 10** |

---

## 2. PHÂN TÍCH CHI TIẾT 4 PHẠM VI AUDIT

### 2.1. Đấu trường từ vựng 1v1 & SignalR GameHub (SP3-DUEL)
- **Vòng đời WebSocket & Đồng bộ 5 Vòng:**
  - Vòng đời phòng đấu (`ActiveDuelRoom`) được quản lý chuẩn qua `RunMatchLifecycleAsync` với 5 câu hỏi đồng bộ, đếm ngược 10 giây/câu.
  - Server-side Anti-Cheat: Payload `DuelRoundQuestionDto` phát xuống client tuyệt đối không chứa `correctKey` hay giải thích trong suốt vòng đấu.
- **Xử lý ngắt kết nối đột ngột & Thời gian ân hạn (<10s):**
  - Khi người chơi mất kết nối, `OnDisconnectedAsync` kích hoạt bộ đếm ân hạn 10 giây (`ReconnectGracePeriodSeconds = 10`) và gửi thông báo `OpponentDisconnected` kèm thời gian chờ.
  - Nếu kết nối lại trong 10 giây, `JoinMatchRoom` bắt được token, hủy timer đếm ngược, phục hồi trạng thái qua `MatchStateSynced` (câu hỏi, vòng hiện tại, thời gian còn lại, điểm số hai bên) và bắn sự kiện `OpponentReconnected`.
  - Nếu quá 10 giây hoặc người chơi bấm `ForfeitMatch`, hệ thống tự động xử thua (`disconnect_timeout` / `player_forfeit`), trao chiến thắng và 30 XP cho người ở lại, giải phóng tài nguyên phòng đấu (`CleanupRoom`) mà không bao giờ làm treo máy người chơi còn lại.
- **Chống Race Condition & Atomic JSONB Append:**
  - Trên PostgreSQL, việc ghi nhận câu trả lời được thực thi bằng câu lệnh SQL nguyên tử `challenger_answers = challenger_answers || @new::jsonb`, loại bỏ hoàn toàn nguy cơ Lost Update khi 2 người chơi submit ở cùng một mili-giây.
  - Kiểm tra trùng lặp chặt chẽ: `playerAnswers.Any(a => a.QuestionId == request.QuestionId)` ngăn chặn triệt để hành vi nộp bài 2 lần trong 1 vòng.
- **Idempotency XP Ledger & Thẻ vinh danh Clan:**
  - Mỗi trận đấu hoàn tất được gán khóa Idempotency Key duy nhất `duel_xp_{matchId}_{userId}` khi gọi qua `IXpService`. Nếu phát hiện trùng lặp, `XpService` từ chối cộng đúp điểm.
  - `PublishedHonorCards` kết hợp với `DuelClanNotifier.PublishedMatches` tạo lớp bảo vệ 2 tầng đảm bảo thẻ vinh danh chỉ phát lên kênh Clan đúng 1 lần duy nhất.

### 2.2. Deep AI Mistake Tutor v2.0 (SP3-AI-TUTOR)
- **Cơ chế Fallback Đa Tầng (Zero-Crash Architecture):**
  - Khi thiếu `Gemini:ApiKey` hoặc gặp lỗi 429 Rate Limit / Timeout / Quá tải mạng, `GeminiAITutorService.ExplainQuizMistakeAsync` không bao giờ ném Exception làm gián đoạn luồng làm bài của học viên.
  - Thay vào đó, service tự động thử xoay vòng qua các candidate models (`gemini-flash-lite-latest`, `gemini-3.5-flash-lite`,...) và chuyển sang bộ phân tích dự phòng tất định (`CreateMistakeFallbackResponse`) với đầy đủ 6 trường thông tin: lý do sai, quy tắc ngữ pháp, từ đồng nghĩa, từ trái nghĩa, mẹo nhớ nhanh và câu ví dụ.
  - Phía Next.js proxy route (`src/web-app/src/app/api/ai/explain-mistake/route.ts`) cài đặt timeout 7 giây (`AbortSignal.timeout(7000)`) và có sẵn bộ `buildFallbackMistakeAnalysis` cục bộ phòng khi Backend chưa khởi động.
- **Phòng chống Prompt Injection & Độ trễ:**
  - Sử dụng tham số `system_instruction` độc lập của Google Gemini API kết hợp `responseMimeType: "application/json"`, nhiệt độ `temperature: 0.25` và độ dài đầu ra giới hạn `maxOutputTokens: 1000`.
  - Giới hạn độ dài dữ liệu đầu vào thông qua Data Annotations (`[MaxLength(1000)]` cho câu hỏi, `[MaxLength(500)]` cho các lựa chọn).

### 2.3. Mezon Bot Ephemeral Cards & Button Interaction Observer
- **Bộ quan sát tương tác nút bấm (`ButtonInteractionObserver`):**
  - Debounce 2 giây ngăn chặn thao tác spam click liên tục từ người dùng.
  - Phân định rõ ràng giữa tin nhắn Single-Use (chỉ cho phép bấm 1 lần trên toàn bộ tin nhắn) và Multi-User (cho phép nhiều người tham gia, mỗi người bấm 1 lần như đố vui Clan 30s).
  - Tự động gọi `channel.UpdateMessageAsync` để chuyển đổi trạng thái nút bấm sang `disabled` (thêm tiền tố `✓` cho nút đã bấm).
  - Bộ nhớ lưu trữ tự động dọn dẹp theo chu kỳ khi danh sách vượt quá 500 tin nhắn hoặc cũ hơn 2 giờ.
- **Bảo mật quyền riêng tư qua tin nhắn Ephemeral:**
  - Đã kiểm tra bản vá commit `51134e1`: lệnh `/streak` và nút bấm `Điểm danh ngay` (`btn_daily_checkin`) được gửi qua `channel.SendEphemeralAsync(content, userId)`.
  - Phản hồi kết quả Quiz trong kênh Clan cũng được gửi kín Ephemeral cho từng người chơi để chống lộ đáp án cho các thành viên khác trong thời gian 30 giây.

### 2.4. Đánh giá Kiểm thử & An toàn Thông tin (OWASP, Secrets, CORS)
- **Kết quả Kiểm thử:**
  - Toàn bộ **107/107 Unit Tests** trong `Lingual.sln` đều PASS 100%:
    - `Lingual.Modules.MezonBot.Tests`: **77/77 passed**
    - `Lingual.Modules.Gamification.Tests`: **23/23 passed**
    - `Lingual.Modules.Identity.Tests`: **7/7 passed**
  - Frontend TypeScript Check (`tsc --noEmit`): **0 lỗi**
  - Frontend Next.js Production Build (`next build`): **Biên dịch thành công 27/27 static/dynamic routes**
- **Rà soát Secrets & Credentials:**
  - `appsettings.json` chỉ chứa các giá trị placeholder chuẩn (`YOUR_DB_USER`, `YOUR_MEZON_BOT_TOKEN`, `YOUR_GEMINI_API_KEY`).
  - Toàn bộ API key và thông tin bí mật thực tế đều nằm trong `appsettings.Local.json` đã được đưa vào `.gitignore`.
- **Cấu hình CORS:**
  - Cấu hình chỉ cho phép danh sách nguồn chỉ định (`http://localhost:3000`, `http://127.0.0.1:3000`, `https://mezon.ai`), không dùng wildcard `AllowAnyOrigin()` khi có `AllowCredentials()`.

---

## 3. RỦI RO TIỀM ẨN PHÁT HIỆN & KHUYẾN NGHỊ CẢI THIỆN

### [RỦI RO 1 - MEDIUM] Khóa tĩnh toàn cục `MatchMutationLock` trong `WordDuelService.cs`
- **Mô tả hiện trạng:**
  Dòng 35 của `WordDuelService.cs` sử dụng `private static readonly SemaphoreSlim MatchMutationLock = new(1, 1);` bọc toàn bộ phương thức `SubmitAnswerAsync`.
- **Nguy cơ:**
  Khi hệ thống chịu tải cao với hàng chục trận đấu diễn ra cùng lúc (ví dụ 50 trận đấu đồng thời), mọi lượt nộp bài của tất cả người chơi đều phải xếp hàng qua 1 khóa duy nhất, tạo thành điểm nghẽn hiệu năng (Lock Contention).
- **Khuyến nghị:**
  Chuyển sang cơ chế Keyed Lock theo từng trận đấu `ConcurrentDictionary<Guid, SemaphoreSlim>` hoặc tận dụng trực tiếp Row-level Lock của PostgreSQL (`UPDATE duel_matches ... WHERE id = @id`).

### [RỦI RO 2 - LOW] Thu dọn bộ nhớ `ConnectionPlayerMap` trong `GameHub.cs`
- **Mô tả hiện trạng:**
  `ConnectionPlayerMap` lưu ánh xạ `Context.ConnectionId -> (MatchId, UserId)`. Khi trận đấu hoàn tất bình thường qua `RunMatchLifecycleAsync`, phương thức `CleanupRoom(matchId)` dọn dẹp `ActiveRooms` nhưng chưa chủ động dọn `ConnectionPlayerMap` cho đến khi kết nối WebSocket thực sự ngắt.
- **Khuyến nghị:**
  Bổ sung bước duyệt dọn dẹp các connectionId thuộc `matchId` vừa kết thúc trong hàm `CleanupRoom` để giữ cấu trúc RAM luôn tối giản nhất.

### [RỦI RO 3 - LOW] Bổ sung bộ lọc ký tự điều khiển đầu vào cho Deep AI Tutor
- **Mô tả hiện trạng:**
  Đề bài và câu trả lời được đưa trực tiếp vào chuỗi user prompt gửi tới Gemini API.
- **Khuyến nghị:**
  Bổ sung hàm cắt tỉa ký tự điều khiển đặc biệt trước khi ghép vào prompt để gia cố khả năng chống các chuỗi câu hỏi có chủ đích Prompt Injection.

---

## 4. KẾT LUẬN & ĐỀ XUẤT HÀNH ĐỘNG
1. **Chất lượng mã nguồn:** Toàn bộ code Sprint 3 tuân thủ nghiêm ngặt chuẩn kiến trúc Modular Monolith, bảo toàn tính bất biến của sổ cái XP Ledger và đảm bảo an toàn đa luồng.
2. **Quy định Git:** Toàn bộ code Sprint 3 đã được lưu trữ an toàn trên local và remote `audit` (`vcongggggg/lingual-audit`). **Tuyệt đối không push lên repo chính `origin` khi chưa có lệnh phê duyệt từ anh Văn Công.**
3. **Sẵn sàng triển khai:** Mã nguồn đã sẵn sàng 100% để bước vào Sprint 4 (Kiểm thử bảo mật chuẩn NCC+ & Stress Testing).
