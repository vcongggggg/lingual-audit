using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lingual.Modules.MezonBot.Services;

/// <summary>
/// Giao diện dịch vụ phát động và quản lý Scheduled Clan Quiz theo khung giờ vàng.
/// </summary>
public interface IScheduledQuizService
{
    /// <summary>
    /// Đăng thông báo trước 1 phút công khai trong kênh ("1 phút nữa có Thử Thách Khung Giờ Vàng...").
    /// Sử dụng DB để chống gửi trùng lặp.
    /// </summary>
    Task<bool> PostWarningAsync(string slotKey, long channelId, string timeSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phát động phiên Quiz khung giờ vàng công khai, kèm nút bấm và ghim tin nhắn.
    /// Sử dụng DB để đảm bảo Idempotency.
    /// </summary>
    Task<ClanQuizSessionDto?> PostScheduledQuizAsync(string slotKey, long channelId, string timeSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chốt phiên Quiz khung giờ vàng, công bố kết quả vinh danh và giải thích chi tiết bằng tin nhắn công khai.
    /// </summary>
    Task<ClanQuizConcludeDto?> ConcludeScheduledQuizAsync(Guid sessionId, long channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dọn dẹp/kết thúc các phiên quiz còn mở nhưng đã quá hạn ClosesAt mà chưa conclude.
    /// </summary>
    Task CleanUpStaleSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra và tự động seed ngân hàng câu hỏi nếu số lượng câu hỏi hiện có < 30.
    /// </summary>
    Task EnsureQuestionBankCapacityAsync(CancellationToken cancellationToken = default);
}
