using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Domain.Models;

public sealed class DailyActivityEntry
{
    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("study_seconds")]
    public int StudySeconds { get; set; }

    [JsonPropertyName("words_count")]
    public int WordsCount { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FreezeHistoryEntry
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

