using System;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class UserStreakService : IUserStreakService
{
    private readonly IGamificationService _gamificationService;
    private readonly IMezonUserResolver _userResolver;
    private readonly ILogger<UserStreakService> _logger;

    public UserStreakService(
        IGamificationService gamificationService,
        IMezonUserResolver userResolver,
        ILogger<UserStreakService> logger)
    {
        _gamificationService = gamificationService;
        _userResolver = userResolver;
        _logger = logger;
    }

    public async Task<DailyCheckinResultDto> ProcessDailyCheckinAsync(string mezonUserId, string displayName)
    {
        try
        {
            var userId = await _userResolver.ResolveUserIdAsync(mezonUserId, displayName);
            return await _gamificationService.CheckInAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UserStreakService] Lỗi khi xử lý daily checkin cho Mezon User {MezonUserId}", mezonUserId);
            return new DailyCheckinResultDto(
                IsSuccess: false,
                AlreadyCheckedInToday: false,
                CurrentDays: 0,
                BestDays: 0,
                XpAwarded: 0,
                FreezeBalance: 0,
                FreezeUsed: false,
                Message: "Đã xảy ra lỗi khi kết nối hệ thống chuỗi ngày học. Vui lòng thử lại sau ít phút!"
            );
        }
    }

    public async Task<UserStreakDto?> GetStreakAsync(string mezonUserId)
    {
        try
        {
            var userId = await _userResolver.ResolveUserIdAsync(mezonUserId);
            var streak = await _gamificationService.GetUserStreakAsync(userId);
            if (streak == null) return null;

            return new UserStreakDto(
                streak.CurrentDays,
                streak.BestDays,
                streak.LastActiveDate,
                streak.FreezeBalance,
                streak.IsActiveToday
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UserStreakService] Lỗi khi lấy thông tin streak cho Mezon User {MezonUserId}", mezonUserId);
            return null;
        }
    }

    public async Task<StreakFreezeResultDto> UseStreakFreezeAsync(string mezonUserId, string displayName)
    {
        try
        {
            var userId = await _userResolver.ResolveUserIdAsync(mezonUserId, displayName);
            return await _gamificationService.UseStreakFreezeAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UserStreakService] Lỗi khi sử dụng Streak Freeze cho Mezon User {MezonUserId}", mezonUserId);
            return new StreakFreezeResultDto(
                IsSuccess: false,
                RemainingFreezes: 0,
                CurrentDays: 0,
                Message: "Đã xảy ra lỗi khi kết nối hệ thống khiên đóng băng. Vui lòng thử lại sau!"
            );
        }
    }
}
