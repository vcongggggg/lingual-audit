using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class SrsReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SrsReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;
    private int _lastRemindedDay = -1;

    public SrsReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SrsReminderBackgroundService> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [SrsReminderBackgroundService] Khởi động BackgroundService quét nhắc nhở SRS và bảo vệ Streak.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam (UTC+7)
                // Nhắc lúc 20:00 hàng ngày (từ 20:00 trở đi và chưa nhắc trong ngày đó)
                if (now.Hour >= 20 && _lastRemindedDay != now.Day)
                {
                    _lastRemindedDay = now.Day;
                    _logger.LogInformation("⏰ [SrsReminderBackgroundService] Đạt mốc 20:00 giờ tối! Tiến hành quét thẻ SRS đến hạn và gửi cảnh báo Streak.");
                    using var scope = _scopeFactory.CreateScope();
                    var reminderService = scope.ServiceProvider.GetRequiredService<ISrsReminderService>();
                    await reminderService.ScanAndSendRemindersAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "❌ [SrsReminderBackgroundService] Lỗi trong chu kỳ quét nhắc nhở SRS.");
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

