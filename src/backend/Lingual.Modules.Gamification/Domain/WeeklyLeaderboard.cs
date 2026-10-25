using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class WeeklyLeaderboard : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public Guid UserId { get; set; }
    public int XpTotal { get; set; }
    public short? Rank { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
