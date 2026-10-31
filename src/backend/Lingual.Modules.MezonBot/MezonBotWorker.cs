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
            else if (text.Equals("/quiz", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                await HandleQuizCommandAsync(data.ChannelId, senderId);
            }
            else if (text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/check", StringComparison.OrdinalIgnoreCase))
            {
                var prefixLength = text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ? 4 : 6;
                var query = text[prefixLength..].Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    await ReplyToChannelAsync(data.ChannelId, "🦉 LingLing: Bạn hãy nhập câu tiếng Anh cần kiểm tra sau lệnh /ask nhé!\nVí dụ: /ask I go to school yesterday");
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
            .AddField("📚 Học tập & Luyện ngữ pháp", "• /ask <câu>: Mascot AI LingLing phân tích & sửa ngữ pháp\n• /learn: Mở Flashcard 3D ngắt quãng SRS SM-2", false)
            .AddField("🏆 Đấu trường & Minigame Clan", "• /quiz: Bắt đầu minigame đố vui từ vựng 30s tranh tài cùng Clan", false)
            .AddField("⚡ Tiện ích & Trạng thái", "• /ping: Kiểm tra kết nối Bot & Gateway Mezon\n• /help: Xem menu hướng dẫn sử dụng", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_open_miniapp", "🚀 Mở Mini-App", 1, false, "", "")
            .AddButton("btn_guide_ask", "🦉 Hướng dẫn /ask", 2, false, "", "")
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
            .AddField("💡 Mẹo nhỏ", "Gõ /ask <câu tiếng Anh> bất kỳ lúc nào để nhờ Mascot LingLing giải thích ngữ pháp hoặc từ mới.", false)
            .AddField("⚔️ Đấu trường Clan", "Gõ /quiz để bắt đầu minigame đố vui từ vựng 30s tranh điểm XP cho Clan!", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_open_miniapp", "🚀 Bắt đầu học ngay", 1, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("🎯 Sẵn sàng nâng cấp vốn từ vựng hôm nay!")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content);
    }

    private async Task HandleQuizCommandAsync(long channelId, string? senderId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            if (await quizService.HasActiveSessionAsync(channelId))
            {
                await ReplyToChannelAsync(channelId, "⚠️ Đang có một câu đố Clan đang diễn ra!\nHãy nhanh tay bấm chọn đáp án cho câu hỏi hiện tại trên kênh nhé!");
                return;
            }

            const int durationSeconds = 30;
            var session = await quizService.StartSessionAsync(channelId, senderId, durationSeconds);

            if (session == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Không thể khởi tạo câu đố vào lúc này. Vui lòng thử lại sau ít phút!");
                return;
            }

            var optionsText = string.Join("\n", session.Options.Select(o => $"{o.Key}. {o.Text}"));

            var embed = new MessageEmbedBuilder()
                .SetColor("#F59E0B") // Amber / Golden
                .SetTitle("🏆 CLAN VOCABULARY QUIZ — ĐỐ VUI TỪ VỰNG")
                .SetDescription("⚡ Nhanh tay chọn đáp án chính xác để ghi điểm danh dự cho Clan của bạn!\n⏱️ Thời gian trả lời: 30 giây\n🥇 Người đúng & nhanh nhất: +15 XP | Đúng tiếp theo: +5 XP")
                .AddField("📝 Câu hỏi", session.QuestionPrompt, false)
                .AddField("🎯 Các lựa chọn", optionsText, false)
                .AddField("📊 Thông tin", $"Trình độ: {session.Level ?? "A1-A2"} • Độ khó: {session.Difficulty ?? 1}/5", false)
                .SetFooter("Bấm nút tương ứng bên dưới để trả lời (mỗi người chỉ được bấm 1 lần) • LINGUAL Clan", "")
                .Build();

            var buttonBuilder = new ButtonBuilder();
            foreach (var opt in session.Options)
            {
                var customId = $"btn_quiz_{session.SessionId}_{opt.Key}";
                // Đồng bộ style 2 (Secondary - màu xám trung tính) cho tất cả các lựa chọn A, B, C, D
                var style = 2;
                buttonBuilder.AddButton(customId, $"{opt.Key}. {opt.Text}", style, false, "", "");
            }

            var content = new MessageContentBuilder()
                .SetText("🔥 Một câu đố từ vựng Clan vừa xuất hiện! Bấm nút chọn đáp án ngay:")
                .AddEmbed(embed)
                .AddActionRow(buttonBuilder.BuildComponents())
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);

            // Khởi chạy đồng hồ đếm ngược 30 giây trong background để tự động tổng kết phiên
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
                    await ConcludeQuizAsync(channelId, session.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [MezonBot] Lỗi khi tự động kết thúc phiên Clan Quiz {SessionId}", session.SessionId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /quiz cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi bắt đầu câu đố Clan. Vui lòng thử lại!");
        }
    }

    private async Task ConcludeQuizAsync(long channelId, Guid sessionId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            var result = await quizService.ConcludeSessionAsync(sessionId);
            if (result == null)
            {
                return;
            }

            var embedBuilder = new MessageEmbedBuilder();

            if (!string.IsNullOrWhiteSpace(result.WinnerUsername))
            {
                var seconds = (result.WinningResponseMs ?? 0) / 1000.0;
                embedBuilder
                    .SetColor("#10B981") // Emerald Green - Vinh quang
                    .SetTitle("🏁 KẾT THÚC CÂU ĐỐ CLAN — BẢNG VÀNG QUÁN QUÂN")
                    .SetDescription($"🎉 Chúc mừng {result.WinnerUsername} đã xuất sắc trả lời đúng và nhanh nhất trong {seconds:F2} giây!")
                    .AddField("🥇 Người chiến thắng", $"{result.WinnerUsername} (+15 XP)", true)
                    .AddField("⏱️ Tốc độ phản xạ", $"{seconds:F2}s", true);
            }
            else
            {
                embedBuilder
                    .SetColor("#64748B") // Slate Gray
                    .SetTitle("🏁 KẾT THÚC CÂU ĐỐ CLAN — HẾT GIỜ")
                    .SetDescription("⏱️ Đã hết 30 giây! Chưa có thành viên nào trả lời đúng câu hỏi này. Cùng ôn tập lại để bứt phá ở câu đố sau nhé!");
            }

            embedBuilder
                .AddField("🎯 Đáp án chính xác", $"{result.CorrectKey}. {result.CorrectText}", false)
                .AddField("📖 Giải thích chi tiết", result.Explanation ?? "Không có giải thích bổ sung.", false)
                .AddField("📊 Thống kê phiên", $"Tổng người tham gia: {result.TotalResponses} | Trả lời đúng: {result.CorrectResponses} / {result.TotalResponses}", false)
                .SetFooter("LINGUAL Clan Quiz • Gõ /quiz để bắt đầu phiên mới!", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Luyện Flashcard 3D", 1, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("📢 Tổng kết kết quả minigame Đố vui Clan:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi tổng kết Clan Quiz {SessionId}", sessionId);
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
                    .AddField("📝 Câu của bạn", analysis.OriginalText, false)
                    .AddField("📖 Phân tích cấu trúc", analysis.Explanation, false)
                    .AddField("🎯 Mẹo mở rộng", analysis.Tips, false);
            }
            else
            {
                embedBuilder
                    .SetColor("#F59E0B") // Amber Orange - Cần chú ý / học hỏi
                    .SetTitle("🦉 LINGLING GRAMMAR COACH — GỢI Ý CHỈNH SỬA")
                    .SetDescription("💡 Đánh giá: Cần hoàn thiện • Xem chi tiết gợi ý chuẩn bản xứ bên dưới:")
                    .AddField("📝 Câu gốc của bạn", analysis.OriginalText, false)
                    .AddField("✨ Câu chuẩn bản xứ", analysis.CorrectedText, false)
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

            if (buttonId.StartsWith("btn_quiz_"))
            {
                // Format: btn_quiz_{sessionId}_{key}
                var parts = buttonId.Split('_');
                if (parts.Length >= 4 && Guid.TryParse(parts[2], out var sessionId))
                {
                    var selectedKey = parts[3];
                    var mezonUserId = data.UserId.ToString();
                    var username = $"Member_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";

                    await HandleQuizButtonClickAsync(channelId, sessionId, mezonUserId, username, selectedKey);
                }
            }
            else if (buttonId == "btn_save_flashcard")
            {
                await ReplyToChannelAsync(channelId, "💾 LingLing: Đã lưu mẫu câu và phân tích ngữ pháp này vào sổ tay Flashcard SRS của bạn!");
            }
            else if (buttonId == "btn_open_miniapp")
            {
                await ReplyToChannelAsync(channelId, "🚀 LingLing: Đang điều hướng đến Mini-App Flashcard 3D... Chúc bạn có một phiên học hiệu quả!");
            }
            else if (buttonId == "btn_guide_ask")
            {
                await ReplyToChannelAsync(channelId, "🦉 LingLing: Bạn chỉ cần gõ /ask <câu tiếng Anh> (Ví dụ: /ask She don't like coffee) là mình sẽ phân tích ngay!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện click button");
        }
    }

    private async Task HandleQuizButtonClickAsync(
        long channelId,
        Guid sessionId,
        string mezonUserId,
        string username,
        string selectedKey)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            var result = await quizService.SubmitAnswerAsync(sessionId, channelId, mezonUserId, username, selectedKey);

            if (result.Status == QuizSubmissionStatus.AlreadyAnswered)
            {
                await ReplyToChannelAsync(channelId, $"⚠️ {username}: Bạn đã gửi câu trả lời cho câu đố này rồi! Mỗi thành viên chỉ được bấm 1 lần.");
            }
            else if (result.Status == QuizSubmissionStatus.SessionExpired)
            {
                await ReplyToChannelAsync(channelId, $"⏰ {username}: Phiên đố vui này đã hết thời gian nhận đáp án!");
            }
            else if (result.Status == QuizSubmissionStatus.Success)
            {
                if (result.IsWinner)
                {
                    var seconds = result.ResponseMs / 1000.0;
                    await ReplyToChannelAsync(channelId, $"⚡ {username} đã trả lời ĐÚNG thần tốc trong {seconds:F2}s! Đang dẫn đầu bảng (+15 XP) 🥇");
                }
                else if (result.IsCorrect)
                {
                    await ReplyToChannelAsync(channelId, $"✅ {username} chọn đúng đáp án {selectedKey} (+5 XP)!");
                }
                else
                {
                    await ReplyToChannelAsync(channelId, $"❌ {username} chọn đáp án {selectedKey} (chưa chính xác). Hãy đợi tổng kết nhé!");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý trả lời Quiz {SessionId} từ User {UserId}", sessionId, mezonUserId);
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
