using System;

namespace Lingual.Modules.Gamification.Domain;

public class GameWordPool
{
    public string Id { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string? Phonetic { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? TopicVi { get; set; }
    public string CefrLevel { get; set; } = "A1";
    public string? ExampleSentence { get; set; }
    public string? ExampleTranslation { get; set; }
    public string Source { get; set; } = "ai-generated";
    public bool Verified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
