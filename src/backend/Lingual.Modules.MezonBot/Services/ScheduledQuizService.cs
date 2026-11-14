using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Mezon.Net.Sdk;
using Mezon.Net.Models;
using Mezon.Net.Sdk.Builders;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;

namespace Lingual.Modules.MezonBot.Services;

/// <summary>
/// Triển khai dịch vụ Scheduled Clan Quiz.
/// </summary>
public class ScheduledQuizService : IScheduledQuizService
{
    private readonly MezonBotDbContext _dbContext;
    private readonly IClanQuizService _clanQuizService;
    private readonly IMezonBotNotifier _notifier;
    private readonly ScheduledQuizOptions _options;
    private readonly ILogger<ScheduledQuizService> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ScheduledQuizService(
        MezonBotDbContext dbContext,
        IClanQuizService clanQuizService,
        IMezonBotNotifier notifier,
        IOptions<ScheduledQuizOptions> options,
        ILogger<ScheduledQuizService> logger,
        IServiceScopeFactory? scopeFactory = null)
    {
        _dbContext = dbContext;
        _clanQuizService = clanQuizService;
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> PostWarningAsync(string slotKey, long channelId, string timeSlot, CancellationToken cancellationToken = default)
    {
        if (channelId <= 0) return false;

        var existingRun = await _dbContext.ScheduledQuizRuns
            .FirstOrDefaultAsync(r => r.SlotKey == slotKey && r.ChannelId == channelId, cancellationToken);

        if (existingRun != null && existingRun.WarnedAt != null)
        {
            _logger.LogInformation("Slot {SlotKey} đã được thông báo trước lúc {WarnedAt} tại Kênh {ChannelId}. Bỏ qua.", slotKey, existingRun.WarnedAt, channelId);
            return false;
        }

        var warningText = $"⏰ **Thông báo:** 1 phút nữa sẽ có Thử Thách Khung Giờ Vàng ({timeSlot}) — cả Clan cùng chuẩn bị nhé!";
        await _notifier.SendPublicChannelTextAsync(channelId, warningText, cancellationToken);

        var now = DateTime.UtcNow;
        if (existingRun == null)
        {
            existingRun = new ScheduledQuizRun
            {
                SlotKey = slotKey,
                ChannelId = channelId,
                WarnedAt = now,
                CreatedAt = now
            };
            _dbContext.ScheduledQuizRuns.Add(existingRun);
        }
        else
        {
            existingRun.WarnedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("📢 [ScheduledQuiz] Đã gửi thông báo trước 1 phút cho Slot {SlotKey} tại Kênh {ChannelId}", slotKey, channelId);
        return true;
    }

    public async Task<ClanQuizSessionDto?> PostScheduledQuizAsync(string slotKey, long channelId, string timeSlot, CancellationToken cancellationToken = default)
    {
        if (channelId <= 0) return null;

        var existingRun = await _dbContext.ScheduledQuizRuns
            .FirstOrDefaultAsync(r => r.SlotKey == slotKey && r.ChannelId == channelId, cancellationToken);

        if (existingRun != null && existingRun.PostedAt != null)
        {
            _logger.LogInformation("Slot {SlotKey} đã được phát động lúc {PostedAt} tại Kênh {ChannelId}. Bỏ qua.", slotKey, existingRun.PostedAt, channelId);
            return null;
        }

        if (await _clanQuizService.HasActiveSessionAsync(channelId))
        {
            _logger.LogWarning("Kênh {ChannelId} đang có phiên đố vui mở. Bỏ qua phát động tự động cho slot {SlotKey}.", channelId, slotKey);
            return null;
        }

        var session = await _clanQuizService.StartSessionAsync(
            channelId: channelId,
            startedByMezonUserId: null,
            durationSeconds: _options.DurationSeconds,
            isPractice: false,
            slotKey: slotKey);

        if (session == null)
        {
            _logger.LogWarning("Không thể khởi tạo phiên đố vui cho slot {SlotKey} tại kênh {ChannelId}", slotKey, channelId);
            return null;
        }

        // 1. Gỡ ghim quiz cũ trên kênh này (nếu có)
        var lastPinnedRun = await _dbContext.ScheduledQuizRuns
            .Where(r => r.ChannelId == channelId && r.PinnedMessageId != null && r.SlotKey != slotKey)
            .OrderByDescending(r => r.PostedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastPinnedRun != null && lastPinnedRun.PinnedMessageId.HasValue)
        {
            try
            {
                await _notifier.DeletePinMessageAsync(channelId, lastPinnedRun.PinnedMessageId.Value, cancellationToken);
                _logger.LogInformation("📌 [ScheduledQuiz] Đã gỡ ghim tin nhắn quiz cũ {MessageId} tại Kênh {ChannelId}", lastPinnedRun.PinnedMessageId.Value, channelId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ScheduledQuiz] Lỗi khi gỡ ghim quiz cũ {MessageId}. Bỏ qua.", lastPinnedRun.PinnedMessageId.Value);
            }
        }

        // 2. Tạo Embed và Nút bấm công khai
        var embedBuilder = new MessageEmbedBuilder()
            .SetColor("#F59E0B")
            .SetTitle($"⏰ THỬ THÁCH KHUNG GIỜ VÀNG ({timeSlot}) — ĐỐ VUI TỪ VỰNG CLAN")
            .SetDescription($"⚡ Nhanh tay chọn đáp án chính xác trong {_options.DurationSeconds} giây để ghi điểm cho Clan và nhận thưởng XP!\n\n**Câu hỏi:**\n{session.QuestionPrompt}")
            .AddField("🥇 Thưởng nhanh nhất", $"+{_options.XpWinner} XP", true)
            .AddField("✅ Trả lời đúng", $"+{_options.XpCorrect} XP", true)
            .AddField("⏱️ Thời gian", $"{_options.DurationSeconds}s", true)
            .SetFooter("Mỗi học viên nhận XP tối đa 1 lần/khung giờ trên toàn hệ thống • LINGUAL Clan Quiz", "");

        var buttonBuilder = new ButtonBuilder();
        foreach (var opt in session.Options)
        {
            buttonBuilder.AddButton(
                $"btn_quiz_{session.SessionId}_{opt.Key}",
                $"{opt.Key}. {opt.Text}",
                1,
                false,
                "",
                "");
        }

        var content = new MessageContentBuilder()
            .SetText($"🔔 **[THỬ THÁCH KHUNG GIỜ VÀNG {timeSlot}]** Cả Clan cùng tham gia trả lời câu đố:")
            .AddEmbed(embedBuilder.Build())
            .AddActionRow(buttonBuilder.BuildComponents())
            .Build();

        var sentMessageId = await _notifier.SendPublicChannelContentAsync(channelId, content, cancellationToken);

        // 3. Ghim tin nhắn mới vừa đăng
        ulong? pinnedId = null;
        if (sentMessageId.HasValue && sentMessageId.Value > 0)
        {
            try
            {
                var pinned = await _notifier.CreatePinMessageAsync(channelId, sentMessageId.Value, cancellationToken);
                if (pinned)
                {
                    pinnedId = sentMessageId.Value;
                    _logger.LogInformation("📌 [ScheduledQuiz] Đã ghim tin nhắn quiz mới {MessageId} tại Kênh {ChannelId}", sentMessageId.Value, channelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ScheduledQuiz] Không thể ghim tin nhắn {MessageId}. Phiên quiz vẫn tiếp tục bình thường.", sentMessageId.Value);
            }
        }

        // 4. Lưu trạng thái vào Database (Chống trùng lặp bằng DB)
        var now = DateTime.UtcNow;
        if (existingRun == null)
        {
            existingRun = new ScheduledQuizRun
            {
                SlotKey = slotKey,
                ChannelId = channelId,
                SessionId = session.SessionId,
                PinnedMessageId = pinnedId,
                PostedAt = now,
                CreatedAt = now
            };
            _dbContext.ScheduledQuizRuns.Add(existingRun);
        }
        else
        {
            existingRun.SessionId = session.SessionId;
            existingRun.PinnedMessageId = pinnedId;
            existingRun.PostedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("🚀 [ScheduledQuiz] Đã phát động thành công Quiz Slot {SlotKey} tại Kênh {ChannelId}", slotKey, channelId);

        // 5. Khởi động hẹn giờ kết thúc phiên đố vui
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.DurationSeconds));
                if (_scopeFactory != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scheduledService = scope.ServiceProvider.GetRequiredService<IScheduledQuizService>();
                    await scheduledService.ConcludeScheduledQuizAsync(session.SessionId, channelId);
                }
                else
                {
                    await ConcludeScheduledQuizAsync(session.SessionId, channelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ScheduledQuiz] Lỗi trong timer tổng kết phiên {SessionId}", session.SessionId);
            }
        });

        return session;
    }

    public async Task<ClanQuizConcludeDto?> ConcludeScheduledQuizAsync(Guid sessionId, long channelId, CancellationToken cancellationToken = default)
    {
        var conclude = await _clanQuizService.ConcludeSessionAsync(sessionId);
        if (conclude == null) return null;

        var embedBuilder = new MessageEmbedBuilder();
        if (!string.IsNullOrWhiteSpace(conclude.WinnerUsername))
        {
            var seconds = (conclude.WinningResponseMs ?? 0) / 1000.0;
            var winnerXpText = conclude.WinnerXpAwarded > 0
                ? $"+{conclude.WinnerXpAwarded} XP"
                : "0 XP (đã nhận ở clan khác)";

            embedBuilder
                .SetColor("#10B981")
                .SetTitle("🏁 KẾT THÚC THỬ THÁCH KHUNG GIỜ VÀNG — BẢNG VINH DANH")
                .SetDescription($"🎉 Chúc mừng **{conclude.WinnerUsername}** đã xuất sắc trả lời ĐÚNG và NHANH NHẤT trong {seconds:F2} giây!")
                .AddField("🥇 Quán quân", $"{conclude.WinnerUsername} ({winnerXpText})", true)
                .AddField("⏱️ Tốc độ", $"{seconds:F2}s", true);
        }
        else
        {
            embedBuilder
                .SetColor("#64748B")
                .SetTitle("🏁 HẾT GIỜ THỬ THÁCH KHUNG GIỜ VÀNG")
                .SetDescription("⏱️ Đã hết 60 giây! Chưa có thành viên nào trả lời đúng câu hỏi này. Cùng ôn tập lại để bứt phá ở khung giờ tiếp theo nhé!");
        }

        embedBuilder
            .AddField("🎯 Đáp án chính xác", $"{conclude.CorrectKey}. {conclude.CorrectText}", false)
            .AddField("📖 Giải thích chi tiết", conclude.Explanation ?? "Không có giải thích bổ sung.", false)
            .AddField("📊 Thống kê tham gia", $"Tổng người tham gia: {conclude.TotalResponses} | Trả lời đúng: {conclude.CorrectResponses} / {conclude.TotalResponses}", false)
            .SetFooter("Đón chờ thử thách khung giờ vàng tiếp theo cùng LingLing!", "");

        var content = new MessageContentBuilder()
            .SetText("📢 **[TỔNG KẾT THỬ THÁCH KHUNG GIỜ VÀNG]**")
            .AddEmbed(embedBuilder.Build())
            .Build();

        await _notifier.SendPublicChannelContentAsync(channelId, content, cancellationToken);
        _logger.LogInformation("🏁 [ScheduledQuiz] Đã tổng kết và gửi kết quả công khai cho phiên {SessionId} tại Kênh {ChannelId}", sessionId, channelId);

        return conclude;
    }

    public async Task CleanUpStaleSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleSessions = await _dbContext.ClanQuizSessions
            .Where(s => s.Status == "open" && s.ClosesAt < now)
            .ToListAsync(cancellationToken);

        if (staleSessions.Count == 0) return;

        _logger.LogInformation("🧹 [ScheduledQuiz] Phát hiện {Count} phiên quiz quá hạn chưa kết thúc. Đang tiến hành kết thúc...", staleSessions.Count);

        foreach (var session in staleSessions)
        {
            try
            {
                var channelId = long.TryParse(session.ChannelMezonId.Replace("dm_", ""), out var cid) ? cid : 0;
                if (channelId > 0 && !session.ChannelMezonId.StartsWith("dm_"))
                {
                    await ConcludeScheduledQuizAsync(session.Id, channelId, cancellationToken);
                }
                else
                {
                    await _clanQuizService.ConcludeSessionAsync(session.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ScheduledQuiz] Không thể dọn dẹp phiên quá hạn {SessionId}", session.Id);
            }
        }
    }

    public async Task EnsureQuestionBankCapacityAsync(CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.QuizQuestions
            .CountAsync(q => q.Status == "published", cancellationToken);

        if (count < 30)
        {
            _logger.LogInformation("📚 [ScheduledQuiz] Ngân hàng câu hỏi hiện có {Count} câu (< 30). Đang tự động nạp ngân hàng câu hỏi chuẩn CEFR...", count);
            var seedQuestions = ScheduledQuizSeedData.GetSeedQuestions();
            foreach (var q in seedQuestions)
            {
                if (!await _dbContext.QuizQuestions.AnyAsync(e => e.Id == q.Id, cancellationToken))
                {
                    _dbContext.QuizQuestions.Add(q);
                }
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            var newCount = await _dbContext.QuizQuestions
                .CountAsync(q => q.Status == "published", cancellationToken);
            _logger.LogInformation("✅ [ScheduledQuiz] Đã nạp thêm câu hỏi thành công. Tổng số câu hỏi hiện tại: {Count}", newCount);
        }
    }
}
