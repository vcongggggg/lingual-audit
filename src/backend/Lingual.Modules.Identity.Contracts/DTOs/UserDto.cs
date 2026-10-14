namespace Lingual.Modules.Identity.Contracts.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string MezonUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string PreferredLocale { get; set; } = "vi-VN";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string Status { get; set; } = "active";
    public string Role { get; set; } = "learner";
    public DateTime? LastSeenAt { get; set; }
    public LearnerProfileDto? Profile { get; set; }
}
