using System;

namespace Lingual.Modules.Gamification.Domain;

public class GameQuestion
{
    public string Id { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? ChoicesJson { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string Level { get; set; } = "A2";
    public string Source { get; set; } = "script-derived";
    public string? SourceRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
