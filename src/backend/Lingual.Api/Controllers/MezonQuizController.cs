using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lingual.Api.Controllers;

[ApiController]
[Route("api/v1/mezon/quiz")]
public class MezonQuizController : ControllerBase
{
    private readonly IScheduledQuizService _scheduledQuizService;
    private readonly ScheduledQuizOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MezonQuizController> _logger;

    // Các channel mặc định của các clan đã biết
    private static readonly long[] DefaultClanChannels = new long[]
    {
        2116390031126958080L, // TESTBOTLINGLING (#general)
        2077639525764435968L  // ĐỤT CẬN TRĨ (#general)
    };

    public MezonQuizController(
        IScheduledQuizService scheduledQuizService,
        IOptions<ScheduledQuizOptions> options,
        IWebHostEnvironment env,
        ILogger<MezonQuizController> logger)
    {
        _scheduledQuizService = scheduledQuizService;
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public class TriggerRequest
    {
        public List<long>? ChannelIds { get; set; }
    }

    /// <summary>
    /// Kích hoạt tức thời Thử Thách Khung Giờ Vàng (Scheduled Clan Quiz) trên đồng thời nhiều Clan.
    /// Chỉ cho phép Admin đã xác thực hoặc gọi nội bộ từ localhost trong môi trường Development.
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerScheduledQuizAcrossClans([FromBody] TriggerRequest? request, CancellationToken cancellationToken)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        var isLocalDev = _env.IsDevelopment() && remoteIp != null && IPAddress.IsLoopback(remoteIp);
        var isAdmin = User?.Identity?.IsAuthenticated == true && User.IsInRole("admin");

        if (!isLocalDev && !isAdmin)
        {
            return Unauthorized(new { error = "Chỉ quản trị viên hoặc kịch bản kiểm thử nội bộ mới có quyền phát động Quiz Clan." });
        }
        var channels = request?.ChannelIds != null && request.ChannelIds.Count > 0
            ? request.ChannelIds
            : DefaultClanChannels.ToList();

        var now = DateTime.UtcNow;
        var tz = _options.GetTimeZone();
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
        var slotStr = $"{nowVn:HH:mm}";
        var triggerSlotKey = $"trigger_{nowVn:yyyy-MM-dd_HH:mm}";

        var results = new List<object>();

        foreach (var channelId in channels)
        {
            try
            {
                var session = await _scheduledQuizService.PostScheduledQuizAsync(triggerSlotKey, channelId, slotStr, cancellationToken);
                results.Add(new
                {
                    ChannelId = channelId,
                    Success = session != null,
                    SessionId = session?.SessionId,
                    SlotKey = triggerSlotKey,
                    Message = session != null ? "Phát động thành công" : "Bỏ qua (kênh đang có phiên mở hoặc đã phát động slot này)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi phát động quiz cho channel {ChannelId}", channelId);
                results.Add(new
                {
                    ChannelId = channelId,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            SlotKey = triggerSlotKey,
            TimeSlot = slotStr,
            TotalClans = channels.Count,
            Results = results
        });
    }
}
