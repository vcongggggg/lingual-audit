using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class Badge : HasDomainEventsBase, IAggregateRoot
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = "common";
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxProgress { get; set; } = 1;
    public int XpReward { get; set; } = 0;
    public string Lore { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
