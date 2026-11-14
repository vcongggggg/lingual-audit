using Lingual.SharedKernel.Entities;

namespace Lingual.Modules.MezonBot.Domain;

public class QuizQuestion : EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? QuizId { get; set; }
    public string QuestionType { get; set; } = "multiple_choice";
    public string Prompt { get; set; } = string.Empty;
    public string? PromptAudioUrl { get; set; }
    public short? Difficulty { get; set; }
    public string? Level { get; set; }
    public string Options { get; set; } = "[]";
    public string Payload { get; set; } = "{}";
    public string? Explanation { get; set; }
    public short Points { get; set; } = 1;
    public short SortOrder { get; set; } = 1;
    public string Status { get; set; } = "published";
    public Guid? CreatedBy { get; set; }
    public DateTime? LastAskedAt { get; set; }

    public Quiz? Quiz { get; set; }
    public ICollection<ClanQuizSession> ClanQuizSessions { get; set; } = new List<ClanQuizSession>();
}
