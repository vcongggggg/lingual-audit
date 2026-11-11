using System.Text.Json;
using Lingual.Modules.Gamification.Common;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Domain.Models;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class StreakService(
    GamificationDbContext dbContext,
    ILogger<StreakService> logger) : IStreakService
{
    private const int STREAK_XP_THRESHOLD = 20;

    public async Task<UserStreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var streak = await dbContext.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var today = TimeZoneHelper.GetCurrentVietnamDate();

        if (streak == null)
        {
            streak = new UserStreak
            {
                UserId = userId,
                CurrentDays = 0,
                BestDays = 0,
                FreezeBalance = 1, // Mặc định 1 freeze ticket theo schema PRD
                LastActiveDate = null,
                DailyActivity = "{}",
                FreezeHistory = "[]",
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.UserStreaks.Add(streak);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Kiểm tra bảo toàn hoặc reset chuỗi nếu đã qua nhiều ngày
            bool modified = EvaluateStreakIntegrity(streak, today);
            if (modified)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        var activities = DeserializeDailyActivity(streak.DailyActivity);
        var todayKey = today.ToString("yyyy-MM-dd");
        int todayXp = activities.TryGetValue(todayKey, out var todayAct) ? todayAct.Xp : 0;
        bool isActiveToday = todayXp >= STREAK_XP_THRESHOLD;
        int xpNeeded = Math.Max(0, STREAK_XP_THRESHOLD - todayXp);

        return new UserStreakDto(
            streak.UserId,
            streak.CurrentDays,
            streak.BestDays,
            streak.LastActiveDate,
            streak.FreezeBalance,
            isActiveToday,
            todayXp,
            xpNeeded,
            streak.UpdatedAt);
    }

    public async Task<UserStreakDto> RecordXpActivityAsync(
        Guid userId,
        int xpDelta,
        int studySeconds = 0,
        int wordsCount = 0,
        CancellationToken cancellationToken = default)
    {
        var streak = await dbContext.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var today = TimeZoneHelper.GetCurrentVietnamDate();

        if (streak == null)
        {
            streak = new UserStreak
            {
                UserId = userId,
                CurrentDays = 0,
                BestDays = 0,
                FreezeBalance = 1,
                LastActiveDate = null,
                DailyActivity = "{}",
                FreezeHistory = "[]",
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.UserStreaks.Add(streak);
        }
        else
        {
            EvaluateStreakIntegrity(streak, today);
        }

        var activities = DeserializeDailyActivity(streak.DailyActivity);
        var freezeHistory = DeserializeFreezeHistory(streak.FreezeHistory);
        var todayKey = today.ToString("yyyy-MM-dd");

        int oldTodayXp = activities.TryGetValue(todayKey, out var existingEntry) ? existingEntry.Xp : 0;
        int newTodayXp = oldTodayXp + Math.Max(0, xpDelta);

        if (existingEntry == null)
        {
            existingEntry = new DailyActivityEntry
            {
                Xp = newTodayXp,
                StudySeconds = studySeconds,
                WordsCount = wordsCount,
                UpdatedAt = DateTime.UtcNow
            };
            activities[todayKey] = existingEntry;
        }
        else
        {
            existingEntry.Xp = newTodayXp;
            existingEntry.StudySeconds += studySeconds;
            existingEntry.WordsCount += wordsCount;
            existingEntry.UpdatedAt = DateTime.UtcNow;
        }

        bool wasActiveToday = oldTodayXp >= STREAK_XP_THRESHOLD;
        bool isActiveNow = newTodayXp >= STREAK_XP_THRESHOLD;

        // Vừa chạm hoặc vượt ngưỡng 20 XP trong ngày hôm nay
        if (!wasActiveToday && isActiveNow)
        {
            ApplyActiveDayStreak(streak, today, freezeHistory, logger);
        }

        streak.DailyActivity = JsonSerializer.Serialize(activities);
        streak.FreezeHistory = JsonSerializer.Serialize(freezeHistory);
        streak.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        int xpNeeded = Math.Max(0, STREAK_XP_THRESHOLD - newTodayXp);

        return new UserStreakDto(
            streak.UserId,
            streak.CurrentDays,
            streak.BestDays,
            streak.LastActiveDate,
            streak.FreezeBalance,
            isActiveNow,
            newTodayXp,
            xpNeeded,
            streak.UpdatedAt);
    }

    public async Task<DailyCheckinResultDto> CheckInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = TimeZoneHelper.GetCurrentVietnamDate();
        var todayKey = today.ToString("yyyy-MM-dd");
        var idempotencyKey = $"daily_checkin_{userId}_{today:yyyyMMdd}";

        var streak = await dbContext.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (streak != null)
        {
            EvaluateStreakIntegrity(streak, today);

            // Kiểm tra xem hôm nay đã điểm danh chưa
            if (streak.LastActiveDate == today)
            {
                return new DailyCheckinResultDto(
                    IsSuccess: true,
                    AlreadyCheckedInToday: true,
                    CurrentDays: streak.CurrentDays,
                    BestDays: streak.BestDays,
                    XpAwarded: 0,
                    FreezeBalance: streak.FreezeBalance,
                    FreezeUsed: false,
                    Message: "Hôm nay bạn đã điểm danh giữ streak rồi! Hãy quay lại vào ngày mai nhé."
                );
            }
        }

        bool alreadyHasLedger = await dbContext.XpLedgers.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (alreadyHasLedger)
        {
            return new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: true,
                CurrentDays: streak?.CurrentDays ?? 0,
                BestDays: streak?.BestDays ?? 0,
                XpAwarded: 0,
                FreezeBalance: streak?.FreezeBalance ?? 1,
                FreezeUsed: false,
                Message: "Hôm nay bạn đã điểm danh giữ streak rồi! Hãy quay lại vào ngày mai nhé."
            );
        }

        const int dailyXp = 20;
        int prevFreeze = streak?.FreezeBalance ?? 1;

        // Cập nhật hoạt động XP vào chuỗi streak
        var updatedStreak = await RecordXpActivityAsync(userId, dailyXp, 0, 0, cancellationToken);
        bool freezeUsed = prevFreeze > updatedStreak.FreezeBalance;

        // Ghi nhận vào sổ cái xp_ledger với idempotency key
        var ledger = new XpLedger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceType = "daily_checkin",
            SourceId = Guid.NewGuid(),
            XpDelta = dailyXp,
            AwardedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey,
            Metadata = JsonSerializer.Serialize(new { type = "daily_checkin", date = todayKey })
        };
        dbContext.XpLedgers.Add(ledger);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("🔥 [StreakService] User {UserId} đã điểm danh thành công ngày {Date}! Chuỗi hiện tại: {Days} ngày",
            userId, todayKey, updatedStreak.CurrentDays);

        return new DailyCheckinResultDto(
            IsSuccess: true,
            AlreadyCheckedInToday: false,
            CurrentDays: updatedStreak.CurrentDays,
            BestDays: updatedStreak.BestDays,
            XpAwarded: dailyXp,
            FreezeBalance: updatedStreak.FreezeBalance,
            FreezeUsed: freezeUsed,
            Message: "Điểm danh hàng ngày thành công! Giữ vững ngọn lửa học tập nhé."
        );
    }

    public async Task<StreakFreezeResultDto> UseStreakFreezeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = TimeZoneHelper.GetCurrentVietnamDate();
        var todayKey = today.ToString("yyyy-MM-dd");

        var streak = await dbContext.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (streak == null)
        {
            streak = new UserStreak
            {
                UserId = userId,
                CurrentDays = 0,
                BestDays = 0,
                FreezeBalance = 1,
                LastActiveDate = null,
                DailyActivity = "{}",
                FreezeHistory = "[]",
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.UserStreaks.Add(streak);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            EvaluateStreakIntegrity(streak, today);
        }

        if (streak.FreezeBalance <= 0)
        {
            return new StreakFreezeResultDto(
                IsSuccess: false,
                RemainingFreezes: 0,
                CurrentDays: streak.CurrentDays,
                Message: "Bạn không còn Khiên đóng băng (Streak Freeze) nào để sử dụng!"
            );
        }

        var freezeHistory = DeserializeFreezeHistory(streak.FreezeHistory);

        // Kiểm tra nếu hôm nay đã kích hoạt khiên đóng băng rồi
        if (freezeHistory.Any(f => f.Date == todayKey && (f.Action == "manual_freeze_activated" || f.Action == "streak_freeze_protection")))
        {
            return new StreakFreezeResultDto(
                IsSuccess: false,
                RemainingFreezes: streak.FreezeBalance,
                CurrentDays: streak.CurrentDays,
                Message: "Bạn đã kích hoạt Khiên đóng băng cho ngày hôm nay rồi!"
            );
        }

        // Tiêu thụ thật 1 khiên đóng băng trong CSDL
        streak.FreezeBalance -= 1;
        freezeHistory.Add(new FreezeHistoryEntry
        {
            Date = todayKey,
            Action = "manual_freeze_activated",
            Reason = "streak_freeze_protection",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });

        // Bảo toàn chuỗi cho ngày hôm nay bằng cách cập nhật LastActiveDate để ngày mai không bị đứt chuỗi
        streak.LastActiveDate = today;
        streak.FreezeHistory = JsonSerializer.Serialize(freezeHistory);
        streak.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("🛡️ [StreakService] User {UserId} đã kích hoạt Khiên đóng băng thành công cho ngày {Date}! Còn lại {Remaining} khiên.",
            userId, todayKey, streak.FreezeBalance);

        return new StreakFreezeResultDto(
            IsSuccess: true,
            RemainingFreezes: streak.FreezeBalance,
            CurrentDays: streak.CurrentDays,
            Message: $"Kích hoạt Khiên đóng băng thành công! Chuỗi {streak.CurrentDays} ngày của bạn đã được bảo vệ an toàn cho ngày hôm nay."
        );
    }

    private static void ApplyActiveDayStreak(UserStreak streak, DateOnly today, List<FreezeHistoryEntry> freezeHistory, ILogger logger)
    {
        if (streak.LastActiveDate == null)
        {
            // Bắt đầu chuỗi lần đầu
            streak.CurrentDays = 1;
            streak.LastActiveDate = today;
            logger.LogInformation("Người dùng {UserId} bắt đầu chuỗi streak 1 ngày", streak.UserId);
        }
        else if (streak.LastActiveDate == today)
        {
            // Đã được đánh dấu active trong ngày hôm nay
            return;
        }
        else if (streak.LastActiveDate == today.AddDays(-1))
        {
            // Nối tiếp hoàn hảo ngày hôm qua
            streak.CurrentDays += 1;
            streak.LastActiveDate = today;
            logger.LogInformation("Người dùng {UserId} tăng chuỗi streak lên {Days} ngày", streak.UserId, streak.CurrentDays);
        }
        else
        {
            // Bỏ lỡ ít nhất 1 ngày
            int missedDays = today.DayNumber - streak.LastActiveDate.Value.DayNumber - 1;

            if (missedDays == 1 && streak.FreezeBalance > 0)
            {
                // Tiêu thụ 1 khiên Streak Freeze
                streak.FreezeBalance -= 1;
                freezeHistory.Add(new FreezeHistoryEntry
                {
                    Date = today.AddDays(-1).ToString("yyyy-MM-dd"),
                    Action = "consumed",
                    Reason = "streak_freeze_protection",
                    Quantity = 1,
                    Timestamp = DateTime.UtcNow
                });

                // Bảo toàn chuỗi và tính ngày hôm nay
                streak.CurrentDays += 1;
                streak.LastActiveDate = today;
                logger.LogInformation("Người dùng {UserId} tiêu thụ 1 Streak Freeze bảo toàn chuỗi {Days} ngày", streak.UserId, streak.CurrentDays);
            }
            else
            {
                // Đứt chuỗi, bắt đầu chuỗi mới
                streak.CurrentDays = 1;
                streak.LastActiveDate = today;
                logger.LogInformation("Người dùng {UserId} reset streak về 1 sau {MissedDays} ngày không active", streak.UserId, missedDays);
            }
        }

        streak.BestDays = Math.Max(streak.BestDays, streak.CurrentDays);
    }

    private static bool EvaluateStreakIntegrity(UserStreak streak, DateOnly today)
    {
        if (streak.LastActiveDate == null || streak.CurrentDays <= 0)
        {
            return false;
        }

        // Nếu ngày active gần nhất là hôm nay hoặc hôm qua thì chuỗi vẫn còn hiệu lực
        if (streak.LastActiveDate >= today.AddDays(-1))
        {
            return false;
        }

        int missedDays = today.DayNumber - streak.LastActiveDate.Value.DayNumber - 1;
        var freezeHistory = DeserializeFreezeHistory(streak.FreezeHistory);

        if (missedDays == 1 && streak.FreezeBalance > 0)
        {
            // Tự động tiêu thụ khiên Streak Freeze cho ngày hôm qua để bảo toàn chuỗi
            streak.FreezeBalance -= 1;
            freezeHistory.Add(new FreezeHistoryEntry
            {
                Date = today.AddDays(-1).ToString("yyyy-MM-dd"),
                Action = "auto_freeze_consumed",
                Reason = "streak_freeze_protection",
                Quantity = 1,
                Timestamp = DateTime.UtcNow
            });

            // Bảo lưu chuỗi tính đến hôm qua
            streak.LastActiveDate = today.AddDays(-1);
            streak.FreezeHistory = JsonSerializer.Serialize(freezeHistory);
            streak.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        // Bỏ lỡ nhiều ngày hoặc hết khiên -> reset chuỗi về 0
        streak.CurrentDays = 0;
        streak.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private static Dictionary<string, DailyActivityEntry> DeserializeDailyActivity(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, DailyActivityEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<FreezeHistoryEntry> DeserializeFreezeHistory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<FreezeHistoryEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
