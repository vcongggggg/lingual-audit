namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record DailyCheckinResultDto(
    bool IsSuccess,
    bool AlreadyCheckedInToday,
    int CurrentDays,
    int BestDays,
    int XpAwarded,
    int FreezeBalance,
    bool FreezeUsed,
    string Message
);
