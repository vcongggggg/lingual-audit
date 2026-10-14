using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Gamification.Domain;

public class DuelMatch : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChallengerId { get; set; }
    public Guid OpponentId { get; set; }
    public string Status { get; set; } = "pending";
    public short QuestionCount { get; set; } = 5;
    public string QuestionIds { get; set; } = "[]";
    public string ChallengerAnswers { get; set; } = "[]";
    public string OpponentAnswers { get; set; } = "[]";
    public decimal ChallengerScore { get; set; }
    public decimal OpponentScore { get; set; }
    public short ChallengerCorrect { get; set; }
    public short OpponentCorrect { get; set; }
    public Guid? WinnerId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
