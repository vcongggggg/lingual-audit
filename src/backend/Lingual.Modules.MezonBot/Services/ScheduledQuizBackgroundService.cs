using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lingual.Modules.MezonBot.Services;

/// <summary>
/// BackgroundService quản lý lịch trình tự động phát động Scheduled Clan Quiz.
/// Chạy định kỳ mỗi phút, sử dụng TimeZoneInfo cấu hình chuẩn và Database Idempotency.
/// </summary>
public class ScheduledQuizBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScheduledQuizOptions _options;
    private readonly ILogger<ScheduledQuizBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public ScheduledQuizBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ScheduledQuizOptions> options,
        ILogger<ScheduledQuizBackgroundService> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("⏸️ [ScheduledQuizBackgroundService] Tính năng Scheduled Clan Quiz đang bị tắt theo cấu hình.");
            return;
        }

        // 1. Fail-Fast: Kiểm tra cấu hình bắt buộc khi bật lịch trình
        _options.Validate();

        _logger.LogInformation("🚀 [ScheduledQuizBackgroundService] Khởi động BackgroundService phát động Scheduled Clan Quiz. Kênh mục tiêu: {ChannelId}, Khung giờ: [{Slots}]",
            _options.TargetChannelId, string.Join(", ", _options.TimeSlots));

        // 2. Tác vụ khởi động: Nạp ngân hàng câu hỏi (>= 30 câu) và dọn dẹp các phiên treo quá hạn ClosesAt
        try
        {
            using var startupScope = _scopeFactory.CreateScope();
            var initService = startupScope.ServiceProvider.GetRequiredService<IScheduledQuizService>();
            await initService.EnsureQuestionBankCapacityAsync(stoppingToken);
            await initService.CleanUpStaleSessionsAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "❌ [ScheduledQuizBackgroundService] Lỗi trong bước kiểm tra khởi động ngân hàng câu hỏi/dọn dẹp phiên treo.");
        }

        // 3. Vòng lặp quét định kỳ mỗi phút
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tz = _options.GetTimeZone();
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

                foreach (var slotStr in _options.TimeSlots)
                {
                    if (!TimeSpan.TryParse(slotStr, out var slotTimeSpan)) continue;

                    var slotTimeVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, slotTimeSpan.Hours, slotTimeSpan.Minutes, 0);
                    var slotKey = $"{nowVn:yyyy-MM-dd}_{slotStr}";

                    // A. Mốc Báo trước 1 phút: [slot - 1 phút, slot)
                    var warningStart = slotTimeVn.AddMinutes(-1);
                    if (nowVn >= warningStart && nowVn < slotTimeVn)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scheduledService = scope.ServiceProvider.GetRequiredService<IScheduledQuizService>();
                        await scheduledService.PostWarningAsync(slotKey, _options.TargetChannelId, slotStr, stoppingToken);
                    }

                    // B. Mốc Phát động Quiz: [slot, slot + 10 phút) (Hỗ trợ Catch-up 10 phút nếu bot khởi động muộn)
                    var catchUpEnd = slotTimeVn.AddMinutes(10);
                    if (nowVn >= slotTimeVn && nowVn < catchUpEnd)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scheduledService = scope.ServiceProvider.GetRequiredService<IScheduledQuizService>();
                        await scheduledService.PostScheduledQuizAsync(slotKey, _options.TargetChannelId, slotStr, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "❌ [ScheduledQuizBackgroundService] Lỗi trong chu kỳ quét lịch trình Scheduled Clan Quiz.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
