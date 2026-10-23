using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Identity.Domain;

public class PlacementTest : HasDomainEventsBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Status { get; set; } = "in_progress";
    public short QuestionCount { get; set; } = 10;
    public short? CorrectCount { get; set; }
    public string? AssessedLevel { get; set; }
    public string AnswersDetail { get; set; } = "[]";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
}
