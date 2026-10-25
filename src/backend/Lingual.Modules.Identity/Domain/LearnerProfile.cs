using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.Identity.Domain;

public class LearnerProfile : EntityBase
{
    public Guid UserId { get; set; }
    public string? LearningGoal { get; set; }
    public short? DailyCommitmentMinutes { get; set; }
    public string? ProficiencyLevel { get; set; }
    public string PlacementStatus { get; set; } = "pending";
    public DateTime? OnboardingCompletedAt { get; set; }

    public User? User { get; set; }
}
