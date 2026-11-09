namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record AwardXpResult(
    bool IsSuccess,
    int AwardedXp,
    int TotalXp,
    int CurrentStreak,
    bool IsStreakActiveToday,
    bool AlreadyAwarded,
    string? Message = null);

