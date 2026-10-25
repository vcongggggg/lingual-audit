using Lingual.SharedKernel.Events;

namespace Lingual.SharedKernel.Entities;

public abstract class EntityBase : HasDomainEventsBase
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}