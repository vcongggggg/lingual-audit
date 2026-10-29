using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.MezonBot.Domain;

public class Quiz : EntityBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string QuizType { get; set; } = "practice";
    public string? Level { get; set; }
    public string Status { get; set; } = "published";
    public int? TimeLimitSeconds { get; set; }
    public short TotalQuestions { get; set; }
    public Guid? CreatedBy { get; set; }

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
