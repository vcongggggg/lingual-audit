using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record DailyProgressDto(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("fullDay")] string FullDay,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("words")] int Words,
    [property: JsonPropertyName("target")] int Target,
    [property: JsonPropertyName("xpEarned")] int XpEarned,
    [property: JsonPropertyName("accuracy")] int Accuracy,
    [property: JsonPropertyName("isToday")] bool IsToday = false);

public sealed record WeeklyActivityResponseDto(
    [property: JsonPropertyName("days")] IReadOnlyList<DailyProgressDto> Days);

public sealed record BadgeDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("rarity")] string Rarity,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("unlocked")] bool Unlocked,
    [property: JsonPropertyName("unlockedDate")] string? UnlockedDate,
    [property: JsonPropertyName("progress")] int Progress,
    [property: JsonPropertyName("maxProgress")] int MaxProgress,
    [property: JsonPropertyName("xpReward")] int XpReward,
    [property: JsonPropertyName("lore")] string Lore);

public sealed record BadgesResponseDto(
    [property: JsonPropertyName("badges")] IReadOnlyList<BadgeDto> Badges);
