using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class XpLedger : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public int XpDelta { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public string? IdempotencyKey { get; set; }
    public string Metadata { get; set; } = "{}";
}
