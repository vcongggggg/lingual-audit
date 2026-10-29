using Lingual.SharedKernel.Events;

namespace Lingual.Modules.Learning.Domain;

public class LessonProgress : HasDomainEventsBase
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string Status { get; set; } = "not_started";
    public DateTime? FirstStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? BestScore { get; set; }
    public string SessionHistory { get; set; } = "[]";

    public Lesson? Lesson { get; set; }
}
