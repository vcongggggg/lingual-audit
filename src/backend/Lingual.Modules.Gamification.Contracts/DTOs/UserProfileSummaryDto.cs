using System.Text.Json.Serialization;

namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record UserStreakSummaryDto(
    [property: JsonPropertyName("currentDays")] int CurrentDays,
    [property: JsonPropertyName("bestDays")] int BestDays,
    [property: JsonPropertyName("freezeBalance")] int FreezeBalance,
    [property: JsonPropertyName("isActiveToday")] bool IsActiveToday,
    [property: JsonPropertyName("todayXp")] int TodayXp,
    [property: JsonPropertyName("xpNeededForStreak")] int XpNeededForStreak);

public sealed record UserPerformanceStatsDto(
    [property: JsonPropertyName("wordsLearned")] int WordsLearned,
    [property: JsonPropertyName("quizzesCompleted")] int QuizzesCompleted,
    [property: JsonPropertyName("accuracyRate")] int AccuracyRate,
    [property: JsonPropertyName("quizzesThisWeek")] int QuizzesThisWeek = 0);

public sealed record UserProfileSummaryDto(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("totalXp")] int TotalXp,
    [property: JsonPropertyName("nextLevelXp")] int NextLevelXp,
    [property: JsonPropertyName("streak")] UserStreakSummaryDto Streak,
    [property: JsonPropertyName("badges")] IReadOnlyList<BadgeDto> Badges,
    [property: JsonPropertyName("stats")] UserPerformanceStatsDto Stats);
