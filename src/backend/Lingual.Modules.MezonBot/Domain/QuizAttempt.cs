using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.MezonBot.Domain;

public class QuizAttempt : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "in_progress";
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public short CorrectCount { get; set; }
    public string AnswersDetail { get; set; } = "[]";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public Quiz? Quiz { get; set; }
}
