using Lingual.SharedKernel.Entities;
using Lingual.SharedKernel.Events;

namespace Lingual.Modules.MezonBot.Domain;

public class ClanQuizSession : HasDomainEventsBase, IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ChannelMezonId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public Guid? StartedBy { get; set; }
    public string Status { get; set; } = "open";
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime ClosesAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? WinningUserId { get; set; }
    public int? WinningResponseMs { get; set; }
    public DateTime? ExplanationPublishedAt { get; set; }
    public string Responses { get; set; } = "[]";

    public QuizQuestion? Question { get; set; }
}
