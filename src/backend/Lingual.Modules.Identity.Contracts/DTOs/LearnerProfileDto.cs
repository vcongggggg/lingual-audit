namespace Lingual.Modules.Identity.Contracts.DTOs;

public class LearnerProfileDto
{
    public Guid UserId { get; set; }
    public string? LearningGoal { get; set; }
    public short? DailyCommitmentMinutes { get; set; }
    public string? ProficiencyLevel { get; set; }
    public string PlacementStatus { get; set; } = "pending";
    public DateTime? OnboardingCompletedAt { get; set; }
}
