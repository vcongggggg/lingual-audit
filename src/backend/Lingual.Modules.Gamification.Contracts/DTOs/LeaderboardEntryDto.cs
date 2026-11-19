using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record LeaderboardEntryDto(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("xp")] double Xp,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl = null,
    [property: JsonPropertyName("level")] int Level = 1,
    [property: JsonPropertyName("words_learned")] int WordsLearned = 0,
    [property: JsonPropertyName("streak_days")] int StreakDays = 0,
    [property: JsonPropertyName("previous_rank")] int? PreviousRank = null);

