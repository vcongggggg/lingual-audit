using System;

namespace Lingual.Modules.Gamification.Domain;

public class ActiveGameAttempt
{
    public string AttemptId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string QuestionsPayloadJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
