using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class UserBadge : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string BadgeId { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; } = false;
    public DateTime? UnlockedAt { get; set; }
    public int CurrentProgress { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
