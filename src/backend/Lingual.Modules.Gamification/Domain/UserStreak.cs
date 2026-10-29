using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class UserStreak : HasDomainEventsBase, IAggregateRoot
{
    public Guid UserId { get; set; }
    public int CurrentDays { get; set; }
    public int BestDays { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int FreezeBalance { get; set; }
    public string FreezeHistory { get; set; } = "[]";
    public string DailyActivity { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
