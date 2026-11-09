using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record LeaderboardEntryDto(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("xp")] double Xp,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl = null);

