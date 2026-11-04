using System;
using System.Threading.Tasks;

namespace Lingual.Modules.MezonBot.Services;

public record DailyCheckinResultDto(
    bool IsSuccess,
    bool AlreadyCheckedInToday,
    int CurrentDays,
    int BestDays,
    int XpAwarded,
    int FreezeBalance,
    bool FreezeUsed,
    string Message
);

public record UserStreakDto(
    int CurrentDays,
    int BestDays,
    DateOnly? LastActiveDate,
    int FreezeBalance,
    bool IsActiveToday
);

public interface IUserStreakService
{
    Task<DailyCheckinResultDto> ProcessDailyCheckinAsync(string mezonUserId, string displayName);
    Task<UserStreakDto?> GetStreakAsync(string mezonUserId);
}

