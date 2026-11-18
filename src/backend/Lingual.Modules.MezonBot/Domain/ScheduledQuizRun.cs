using System;
using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.MezonBot.Domain;

/// <summary>
/// Đại diện cho một lượt chạy Scheduled Clan Quiz theo khung giờ.
/// Khóa chính SlotKey (ví dụ: "2026-11-14_09:00") dùng để đảm bảo Idempotency ở tầng Database.
/// </summary>
public class ScheduledQuizRun
{
    public string SlotKey { get; set; } = string.Empty;
    public long ChannelId { get; set; }
    public Guid? SessionId { get; set; }
    public ulong? PinnedMessageId { get; set; }
    public DateTime? WarnedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
