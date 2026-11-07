using System;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.MezonBot.Services;

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
