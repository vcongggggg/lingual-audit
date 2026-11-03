namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record UserStreakDto(
    Guid UserId,
    int CurrentDays,
    int BestDays,
    DateOnly? LastActiveDate,
    int FreezeBalance,
    bool IsActiveToday,
    int TodayXp,
    int XpNeededForStreak,
    DateTime UpdatedAt);

