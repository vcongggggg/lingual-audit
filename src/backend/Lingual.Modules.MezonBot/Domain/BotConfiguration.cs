using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.MezonBot.Domain;

public class BotConfiguration : HasDomainEventsBase, IAggregateRoot
{
    public short Id { get; set; } = 1;
    public string MezonClanId { get; set; } = string.Empty;
    public string? ClanNameSnapshot { get; set; }
    public string? DefaultChannelMezonId { get; set; }
    public string Status { get; set; } = "active";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public Guid? InstalledBy { get; set; }
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime? UninstalledAt { get; set; }
    public bool DailyWordEnabled { get; set; } = true;
    public bool QuizEnabled { get; set; } = true;
    public short QuizDurationSeconds { get; set; } = 30;
    public string Schedules { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
