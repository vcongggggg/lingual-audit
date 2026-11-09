using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Mezon.Net.Sdk;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Mezon.Net.Sdk.Builders;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.MezonBot.Services;
using SdkClient = Mezon.Net.Sdk.MezonClient;

namespace Lingual.Modules.MezonBot;

public class MezonBotWorker : BackgroundService
{
    private readonly ILogger<MezonBotWorker> _logger;
    private readonly MezonBotOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private SdkClient? _client;

    public MezonBotWorker(
        ILogger<MezonBotWorker> logger,
        IOptions<MezonBotOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botId = _options.GetEffectiveBotId();
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Token) || botId <= 0)
        {
            _logger.LogWarning("⚠️ [MezonBot] Chưa cấu hình Mezon:Token hoặc BotId/AppId hợp lệ. Bot worker sẽ ở chế độ chờ và không kết nối gateway.");
            return;
        }

        var retryCount = 0;
        const int baseDelaySeconds = 5;
        const int maxDelaySeconds = 60;

        while (!stoppingToken.IsCancellationRequested)
        {
            var sessionClosedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task OnConnectedAsync()
            {
                _logger.LogInformation("✅ [MezonBot] Kết nối Gateway Mezon thành công/đã phục hồi!");
                retryCount = 0;
                return Task.CompletedTask;
            }

            Task OnDisconnectedAsync(Exception? ex)
            {
                _logger.LogWarning(ex, "⚠️ [MezonBot] Mất kết nối tới Gateway Mezon. Kích hoạt chu trình tái kết nối...");
                sessionClosedTcs.TrySetResult(true);
                return Task.CompletedTask;
            }

            Task OnReconnectingAsync(Exception? ex)
            {
                _logger.LogInformation("🔄 [MezonBot] SDK đang cố gắng tự tái kết nối Gateway Mezon...");
                return Task.CompletedTask;
            }

            try
            {
                _logger.LogInformation("🚀 [MezonBot] Đang khởi tạo kết nối Mezon.Net.Sdk 1.7.0 cho BotId: {BotId}...", botId);

                var clientOptions = new MezonClientOptions(botId, _options.Token);
                _client = new SdkClient(clientOptions);

                _client.Connected += OnConnectedAsync;
                _client.Disconnected += OnDisconnectedAsync;
                _client.Reconnecting += OnReconnectingAsync;
                _client.ChannelMessageReceived += OnChannelMessageReceivedAsync;
                _client.MessageButtonClicked += OnMessageButtonClickedAsync;

                await _client.LoginAsync();
                _logger.LogInformation("✅ [MezonBot] Đã đăng nhập thành công vào Gateway Mezon!");

                retryCount = 0;

                // Chờ cho đến khi phiên kết nối bị ngắt hoặc dịch vụ nhận lệnh dừng
                using (stoppingToken.Register(() => sessionClosedTcs.TrySetCanceled()))
                {
                    await sessionClosedTcs.Task;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🛑 [MezonBot] Nhận tín hiệu dừng service.");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                var delaySeconds = Math.Min(baseDelaySeconds * (int)Math.Pow(2, Math.Min(retryCount - 1, 4)), maxDelaySeconds);
                _logger.LogError(ex, "❌ [MezonBot] Lỗi Gateway hoặc mất kết nối Mezon Bot (Lần {RetryCount}). Thử kết nối lại sau {Delay}s...", retryCount, delaySeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            finally
            {
                if (_client != null)
                {
                    try
                    {
                        _client.Connected -= OnConnectedAsync;
                        _client.Disconnected -= OnDisconnectedAsync;
                        _client.Reconnecting -= OnReconnectingAsync;
                        _client.ChannelMessageReceived -= OnChannelMessageReceivedAsync;
                        _client.MessageButtonClicked -= OnMessageButtonClickedAsync;
                        await _client.DisposeAsync();
                    }
                    catch (Exception disposeEx)
                    {
                        _logger.LogDebug(disposeEx, "Lỗi khi giải phóng MezonClient cũ.");
                    }
                    finally
                    {
                        _client = null;
                        _logger.LogInformation("🔌 [MezonBot] Đã dọn dẹp phiên kết nối MezonClient.");
                    }
                }
            }
        }
    }

    private async Task OnChannelMessageReceivedAsync(ChannelMessageEventData eventData)
    {
        try
        {
            ChannelMessageResponse data = (ChannelMessageResponse)eventData;
            var rawContent = data.Content;
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return;
            }

            var parsed = MessageContent.Parse(rawContent);
            var text = parsed.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _logger.LogInformation("📩 [MezonBot] Nhận tin nhắn từ Channel {ChannelId}: '{Text}'", data.ChannelId, text);

            if (text.Equals("/ping", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyToChannelAsync(data.ChannelId, "🏓 Pong! Bot LINGUAL hoạt động bình thường trên nền tảng Mezon Campus Studio 2026. (SDK: Mezon.Net.Sdk 1.7.0)");
            }
            else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                await HandleHelpCommandAsync(data.ChannelId);
            }
            else if (text.Equals("/learn", StringComparison.OrdinalIgnoreCase))
            {
                await HandleLearnCommandAsync(data.ChannelId);
            }
            else if (text.Equals("/streak", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                var displayName = $"Learner_{senderId[..Math.Min(6, senderId.Length)]}";
                await HandleStreakCommandAsync(data.ChannelId, senderId, displayName);
            }
            else if (text.Equals("/top", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/leaderboard", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/bxh", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                await HandleLeaderboardCommandAsync(data.ChannelId, senderId);
            }
            else if (text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/check", StringComparison.OrdinalIgnoreCase))
            {
                var prefixLength = text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ? 4 : 6;
                var query = text[prefixLength..].Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    await ReplyToChannelAsync(data.ChannelId, "🦉 LingLing: Bạn hãy nhập câu tiếng Anh cần kiểm tra sau lệnh `/ask` nhé!\nVí dụ: `/ask I go to school yesterday`");
                    return;
                }

                await HandleAskGrammarAsync(data.ChannelId, query);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý tin nhắn sự kiện");
        }
    }

    private async Task HandleHelpCommandAsync(long channelId)
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#3B82F6") // Blue Accent
            .SetTitle("🤖 LINGUAL BOT — TRỢ LÝ HỌC NGÔN NGỮ")
            .SetDescription("Chào mừng bạn đến với LINGUAL trên nền tảng Mezon Campus Studio 2026!")
            .AddField("📚 Học tập & Luyện ngữ pháp", "• `/ask <câu>`: Mascot AI LingLing phân tích & sửa ngữ pháp\n• `/learn`: Mở Flashcard 3D ngắt quãng SRS SM-2", false)
            .AddField("🔥 Thi đua & Thói quen", "• `/streak`: Tra cứu chuỗi học tập liên tục & tiến độ XP hôm nay\n• `/top`: Bảng xếp hạng tuần Top 10 cao thủ LINGUAL", false)
            .AddField("⚡ Tiện ích & Trạng thái", "• `/ping`: Kiểm tra kết nối Bot & Gateway Mezon\n• `/help`: Xem menu hướng dẫn sử dụng", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_open_miniapp", "🚀 Mở Mini-App", 1, false, "", "")
            .AddButton("btn_view_streak", "🔥 Chuỗi của tôi", 2, false, "", "")
            .AddButton("btn_view_leaderboard", "🏆 Bảng xếp hạng", 3, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("👋 Danh sách các tính năng khả dụng:")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content);
    }

    private async Task HandleLearnCommandAsync(long channelId)
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#6366F1") // Indigo Accent
            .SetTitle("📚 HỌC TỪ VỰNG TIẾNG ANH CÙNG LINGUAL")
            .SetDescription("Bắt đầu phiên ôn tập thẻ ghi nhớ Flashcard 3D với thuật toán ngắt quãng Spaced Repetition SM-2!")
            .AddField("💡 Mẹo nhỏ", "Gõ `/ask <câu tiếng Anh>` bất kỳ lúc nào để nhờ Mascot LingLing giải thích ngữ pháp hoặc từ mới.", false)
            .AddField("⚔️ Đấu trường Clan", "Chế độ 1vs1 Word Duel giữa các thành viên Clan sắp ra mắt trong Sprint 2!", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_open_miniapp", "🚀 Bắt đầu học ngay", 1, false, "", "")
            .AddButton("btn_view_streak", "🔥 Xem Streak", 2, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("🎯 Sẵn sàng nâng cấp vốn từ vựng hôm nay!")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content);
    }

    private async Task HandleStreakCommandAsync(long channelId, string mezonUserId, string? displayName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
            var gamificationService = scope.ServiceProvider.GetService<IGamificationService>();

            if (gamificationService == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ Gamification (Streak/Leaderboard) chưa sẵn sàng trên hệ thống.");
                return;
            }

            var userId = await userResolver.ResolveUserIdAsync(mezonUserId, displayName);
            var streak = await gamificationService.GetUserStreakAsync(userId);

            var embedBuilder = new MessageEmbedBuilder()
                .SetColor("#F59E0B") // Amber / Flame Orange
                .SetTitle("🔥 CHUỖI HỌC TẬP (STREAK) — LINGUAL")
                .SetDescription($"Chào {displayName ?? "bạn"}, đây là trạng thái chuỗi thói quen học tập của bạn:")
                .AddField("🔥 Chuỗi hiện tại", $"{streak.CurrentDays} ngày liên tục\n(Kỷ lục: {streak.BestDays} ngày)", true)
                .AddField("🛡️ Khiên đóng băng (Freeze)", $"{streak.FreezeBalance} lượt khả dụng\n(Tự động kích hoạt khi lỡ quên học)", true);

            var targetXp = streak.TodayXp + streak.XpNeededForStreak;
            if (targetXp <= 0) targetXp = 20;

            var progressPct = targetXp > 0 ? Math.Min(100, (int)Math.Round((double)streak.TodayXp / targetXp * 100)) : 100;
            var filledBlocks = Math.Min(10, progressPct / 10);
            var emptyBlocks = Math.Max(0, 10 - filledBlocks);
            var progressBar = new string('█', filledBlocks) + new string('░', emptyBlocks);

            var progressMessage = streak.IsActiveToday
                ? $"✅ Đã hoàn thành mục tiêu ngày! ({streak.TodayXp}/{targetXp} XP)\n`[{progressBar}] {progressPct}%` — Chuỗi ngày đã được bảo vệ trọn vẹn hôm nay! 🎉"
                : $"⚡ Tiến độ hôm nay: {streak.TodayXp}/{targetXp} XP\n`[{progressBar}] {progressPct}%`\n⚠️ Cần thêm ít nhất {streak.XpNeededForStreak} XP trước 23:59 để giữ chuỗi!";

            embedBuilder.AddField("⚡ Mục tiêu tích lũy ngày", progressMessage, false);
            embedBuilder.SetFooter("Học đều đặn mỗi ngày để phản xạ tiếng Anh tự nhiên • LINGUAL", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Học ngay", 1, false, "", "")
                .AddButton("btn_view_leaderboard", "🏆 Bảng xếp hạng", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText($"🔥 Cố lên! Bạn đang duy trì chuỗi {streak.CurrentDays} ngày học liên tiếp.")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /streak cho user {MezonUserId}", mezonUserId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Có chút gián đoạn khi kiểm tra chuỗi streak. Bạn vui lòng thử lại sau giây lát nhé!");
        }
    }

    private async Task HandleLeaderboardCommandAsync(long channelId, string mezonUserId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
            var gamificationService = scope.ServiceProvider.GetService<IGamificationService>();

            if (gamificationService == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ Gamification (Leaderboard) chưa sẵn sàng trên hệ thống.");
                return;
            }

            var userId = await userResolver.ResolveUserIdAsync(mezonUserId);
            var leaderboard = await gamificationService.GetLeaderboardAsync(limit: 10, type: "weekly", currentUserId: userId);

            var embedBuilder = new MessageEmbedBuilder()
                .SetColor("#EAB308") // Yellow Gold
                .SetTitle("🏆 BẢNG XẾP HẠNG TUẦN — LINGUAL LEADERBOARD")
                .SetDescription("Vinh danh những chiến binh chăm chỉ nhất trong tuần này (GMT+7):\n");

            if (leaderboard.Entries.Count == 0)
            {
                embedBuilder.AddField("Chưa có dữ liệu", "Chưa có ai ghi điểm tuần này. Hãy là người đầu tiên bứt phá!", false);
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var entry in leaderboard.Entries)
                {
                    var medal = entry.Rank switch
                    {
                        1 => "🥇",
                        2 => "🥈",
                        3 => "🥉",
                        _ => $"#{entry.Rank}"
                    };
                    sb.AppendLine($"{medal} {entry.Name} — `{(long)entry.Xp}` XP");
                }
                embedBuilder.AddField("👑 Top 10 cao thủ tuần", sb.ToString().TrimEnd(), false);
            }

            if (leaderboard.MyRank != null)
            {
                var myEntry = leaderboard.MyRank;
                var rankDisplay = myEntry.Rank > 0 ? $"#{myEntry.Rank}" : "Chưa có hạng";
                embedBuilder.AddField("📍 Vị trí của bạn", $"Hạng: {rankDisplay} • Điểm: {(long)myEntry.Xp} XP", false);
            }

            embedBuilder.SetFooter("Bảng xếp hạng cập nhật thời gian thực qua Redis • LINGUAL", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Luyện tập tích XP", 1, false, "", "")
                .AddButton("btn_view_streak", "🔥 Chuỗi của tôi", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🏆 Bảng vàng vinh danh LINGUAL tuần này:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /top cho user {MezonUserId}", mezonUserId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Đang gặp gián đoạn khi tải bảng xếp hạng. Bạn vui lòng thử lại sau nhé!");
        }
    }

    private async Task HandleAskGrammarAsync(long channelId, string userQuery)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aiTutor = scope.ServiceProvider.GetService<IAITutorService>();

            if (aiTutor == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ AI LingLing chưa sẵn sàng trên hệ thống.");
                return;
            }

            var analysis = await aiTutor.AnalyzeGrammarAsync(new GrammarAnalysisRequest(userQuery));

            var embedBuilder = new MessageEmbedBuilder();

            if (analysis.IsCorrect)
            {
                embedBuilder
                    .SetColor("#10B981") // Emerald Green - Chính xác
                    .SetTitle("🌟 LINGLING GRAMMAR COACH — CHUẨN XÁC")
                    .SetDescription("✨ Đánh giá: Hoàn hảo (100%) • Bạn diễn đạt rất chuẩn và tự nhiên!")
                    .AddField("📝 Câu của bạn", $"`{analysis.OriginalText}`", false)
                    .AddField("📖 Phân tích cấu trúc", analysis.Explanation, false)
                    .AddField("🎯 Mẹo mở rộng", analysis.Tips, false);
            }
            else
            {
                embedBuilder
                    .SetColor("#F59E0B") // Amber Orange - Cần chú ý / học hỏi
                    .SetTitle("🦉 LINGLING GRAMMAR COACH — GỢI Ý CHỈNH SỬA")
                    .SetDescription("💡 Đánh giá: Cần hoàn thiện • Xem chi tiết gợi ý chuẩn bản xứ bên dưới:")
                    .AddField("📝 Câu gốc của bạn", $"`{analysis.OriginalText}`", false)
                    .AddField("✨ Câu chuẩn bản xứ", $"{analysis.CorrectedText}", false)
                    .AddField("📖 Giải thích ngữ pháp", analysis.Explanation, false)
                    .AddField("🎯 Mẹo ghi nhớ", analysis.Tips, false);
            }

            embedBuilder.SetFooter("Powered by LINGUAL AI • Mezon Campus Studio 2026", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_save_flashcard", "💾 Lưu vào Flashcard", 1, false, "", "")
                .AddButton("btn_open_miniapp", "🚀 Mở Mini-App", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🦉 LingLing: Mình đã chấm câu của bạn xong! Cùng xem phân tích chi tiết nhé:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /ask cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Xin lỗi bạn, mình đang gặp chút gián đoạn khi kết nối AI. Bạn hãy thử lại sau ít giây nhé!");
        }
    }

    private async Task OnMessageButtonClickedAsync(MessageButtonClickedEventData eventData)
    {
        try
        {
            MessageButtonClickedResponse data = (MessageButtonClickedResponse)eventData;
            var buttonId = data.ButtonId;
            var channelId = data.ChannelId;

            _logger.LogInformation("🔘 [MezonBot] Người dùng bấm nút {ButtonId} tại Channel {ChannelId}", buttonId, channelId);

            if (buttonId == "btn_save_flashcard")
            {
                await ReplyToChannelAsync(channelId, "💾 LingLing: Đã lưu mẫu câu và phân tích ngữ pháp này vào sổ tay Flashcard SRS của bạn!");
            }
            else if (buttonId == "btn_open_miniapp")
            {
                await ReplyToChannelAsync(channelId, "🚀 LingLing: Đang điều hướng đến Mini-App Flashcard 3D... Chúc bạn có một phiên học hiệu quả!");
            }
            else if (buttonId == "btn_guide_ask")
            {
                await ReplyToChannelAsync(channelId, "🦉 LingLing: Bạn chỉ cần gõ `/ask <câu tiếng Anh>` (Ví dụ: `/ask She don't like coffee`) là mình sẽ phân tích ngay!");
            }
            else if (buttonId == "btn_view_streak")
            {
                var mezonUserId = data.UserId.ToString();
                var displayName = $"Learner_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";
                await HandleStreakCommandAsync(channelId, mezonUserId, displayName);
            }
            else if (buttonId == "btn_view_leaderboard")
            {
                var mezonUserId = data.UserId.ToString();
                await HandleLeaderboardCommandAsync(channelId, mezonUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện click button");
        }
    }

    private async Task ReplyToChannelWithContentAsync(long channelId, MessageContent content)
    {
        if (_client == null || channelId <= 0)
        {
            return;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendAsync(content);
                _logger.LogInformation("📤 [MezonBot] Đã gửi phản hồi dạng Card/Embed đến Channel {ChannelId}", channelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Không thể gửi tin nhắn Card/Embed tới channel {ChannelId}", channelId);
        }
    }

    private async Task ReplyToChannelAsync(long channelId, string responseText)
    {
        if (_client == null || channelId <= 0)
        {
            return;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendTextAsync(responseText);
                _logger.LogInformation("📤 [MezonBot] Đã gửi phản hồi đến Channel {ChannelId}: '{Response}'", channelId, responseText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Không thể gửi tin nhắn phản hồi tới channel {ChannelId}", channelId);
        }
    }
}
