using System;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Learning.Contracts;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class SrsReminderService : ISrsReminderService
{
    private readonly IFlashcardService _flashcardService;
    private readonly IGamificationService _gamificationService;
    private readonly IMezonBotNotifier _notifier;
    private readonly ILogger<SrsReminderService> _logger;

    public SrsReminderService(
        IFlashcardService flashcardService,
        IGamificationService gamificationService,
        IMezonBotNotifier notifier,
        ILogger<SrsReminderService> logger)
    {
        _flashcardService = flashcardService;
        _gamificationService = gamificationService;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<SrsReminderScanResult> ScanAndSendRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueUsers = await _flashcardService.GetUsersWithDueCardsAsync(now, cancellationToken);

        int eligibleCount = 0;
        int remindersSent = 0;

        foreach (var userId in dueUsers)
        {
            try
            {
                var streak = await _gamificationService.GetUserStreakAsync(userId, cancellationToken);
                // Chỉ nhắc học viên khi chưa hoàn thành mục tiêu giữ streak hôm nay
                if (streak.IsActiveToday)
                {
                    continue;
                }

                eligibleCount++;

                var mapping = _notifier.GetUserChannelMapping(userId);
                if (mapping.HasValue)
                {
                    var dueCards = await _flashcardService.GetDueCardsAsync(userId, 5, cancellationToken);
                    bool sent = await _notifier.SendSrsReminderAsync(
                        userId,
                        mapping.Value.channelId,
                        mapping.Value.mezonUserId,
                        dueCards.Count,
                        streak.CurrentDays,
                        streak.FreezeBalance,
                        cancellationToken);

                    if (sent)
                    {
                        remindersSent++;
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ [SrsReminderService] User {UserId} đủ điều kiện nhắc nhở SRS nhưng chưa có channel mapping hoạt động.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SrsReminderService] Lỗi khi xử lý nhắc nhở SRS cho User {UserId}", userId);
            }
        }

        _logger.LogInformation("📊 [SrsReminderService] Quét hoàn tất: {Total} users có thẻ đến hạn, {Eligible} users có nguy cơ đứt streak, {Sent} nhắc nhở đã gửi.",
            dueUsers.Count, eligibleCount, remindersSent);

        return new SrsReminderScanResult(dueUsers.Count, eligibleCount, remindersSent);
    }
}

