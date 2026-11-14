using System;

namespace Lingual.Modules.MezonBot.Domain;

/// <summary>
/// Ghi nhận lượt tham gia Quiz theo lịch của người dùng.
/// Ràng buộc UNIQUE(SlotKey, UserId) đảm bảo 1 user chỉ được nhận XP tối đa 1 lần/khung giờ trên toàn bộ các Clan.
/// </summary>
public class ScheduledQuizParticipation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SlotKey { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string MezonUserId { get; set; } = string.Empty;
    public long ClanId { get; set; }
    public long ChannelId { get; set; }
    public Guid SessionId { get; set; }
    public bool IsCorrect { get; set; }
    public int XpAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
