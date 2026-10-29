using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Identity.Domain;

public class User : EntityBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MezonUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string PreferredLocale { get; set; } = "vi-VN";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string Status { get; set; } = "active";
    public string Role { get; set; } = "learner";
    public DateTime? LastSeenAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public LearnerProfile? Profile { get; set; }
    public ICollection<PlacementTest> PlacementTests { get; set; } = new List<PlacementTest>();
}
