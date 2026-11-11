namespace Lingual.Modules.Gamification.Contracts.DTOs;

public record StreakFreezeResultDto(
    bool IsSuccess,
    int RemainingFreezes,
    int CurrentDays,
    string Message
);

