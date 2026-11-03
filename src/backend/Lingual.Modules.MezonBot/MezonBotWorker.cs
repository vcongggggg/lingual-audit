using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
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

    public class UserRoleplaySession
    {
        public Guid ConversationId { get; set; }
        public string ScenarioCode { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }

    public static readonly ConcurrentDictionary<string, UserRoleplaySession> ActiveRoleplaySessions = new();
    public static readonly ConcurrentDictionary<Guid, CancellationTokenSource> ActiveQuizTimers = new();

    public static string GetRoleplaySessionKey(long channelId, string senderId) => $"{channelId}_{senderId}";

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

            _logger.LogInformation("📩 [MezonBot] Nhận tin nhắn từ Channel {ChannelId} (Clan {ClanId}, Public: {IsPublic}): '{Text}'", data.ChannelId, data.ClanId, data.IsPublic, text);
            var isDm = data.ClanId <= 0 || !data.IsPublic;

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
                await HandleQuizCommandAsync(data.ChannelId, senderId, isDm);
            }
            else if (text.StartsWith("/roleplay", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/scenario", StringComparison.OrdinalIgnoreCase))
            {
                var prefixLength = text.StartsWith("/roleplay", StringComparison.OrdinalIgnoreCase) ? 9 : 9;
                var query = text[prefixLength..].Trim();
                var senderId = data.SenderId.ToString();
                await HandleRoleplayCommandAsync(data.ChannelId, senderId, query);
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
            .SetTitle("🤖 LINGUAL BOT — TRỢ LÝ HỌC TIẾNG ANH")
            .SetDescription("Chào mừng bạn đến với LINGUAL trên nền tảng Mezon Campus Studio 2026!")
            .AddField("📚 Học tập & Luyện ngữ pháp", "• /ask <câu>: Trợ lý LingLing phân tích & sửa lỗi ngữ pháp\n• /roleplay: Đóng vai luyện giao tiếp tiếng Anh theo tình huống thực tế\n• /learn: Ôn tập từ vựng qua thẻ ghi nhớ 3D thông minh", false)
            .AddField("🏆 Đấu trường Clan", "• /quiz: Tham gia đố vui từ vựng 30 giây tranh tài cùng Clan", false)
            .AddField("⚡ Tiện ích & Trạng thái", "• /ping: Kiểm tra kết nối của Bot\n• /help: Xem danh sách lệnh trợ giúp", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_roleplay_menu", "🎭 Luyện Roleplay", 1, false, "", "")
            .AddButton("btn_open_miniapp", "🚀 Học từ vựng", 2, false, "", "")
            .AddButton("btn_guide_ask", "🦉 Hướng dẫn /ask", 2, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("👋 Danh sách các tính năng học tập:")
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
            .SetDescription("Ôn tập từ vựng qua thẻ ghi nhớ 3D thông minh, giúp bạn nhớ sâu và lâu quên hơn mỗi ngày!")
            .AddField("💡 Mẹo nhỏ", "Gõ /ask <câu tiếng Anh> bất kỳ lúc nào để nhờ trợ lý LingLing giải thích ngữ pháp hoặc từ mới.", false)
            .AddField("⚔️ Đấu trường Clan", "Gõ /quiz để tham gia đố vui từ vựng 30 giây cùng Clan và nhận điểm thưởng XP!", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_open_miniapp", "🚀 Bắt đầu học ngay", 1, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("🎯 Cùng LingLing tích lũy từ vựng mỗi ngày:")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content);
    }

    private async Task HandleQuizCommandAsync(long channelId, string? senderId, bool isPractice = false)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            if (await quizService.HasActiveSessionAsync(channelId, isPractice))
            {
                var busyMsg = isPractice
                    ? "⚠️ Bạn đang có một câu đố luyện tập chưa kết thúc!\nHãy chọn đáp án cho câu hỏi hiện tại trước nhé!"
                    : "⚠️ Đang có một câu đố Clan đang diễn ra!\nHãy nhanh tay bấm chọn đáp án cho câu hỏi hiện tại trên kênh nhé!";
                await ReplyToChannelAsync(channelId, busyMsg);
                return;
            }

            const int durationSeconds = 30;
            var session = await quizService.StartSessionAsync(channelId, senderId, durationSeconds, isPractice);

            if (session == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Không thể khởi tạo câu đố vào lúc này. Vui lòng thử lại sau ít phút!");
                return;
            }

            var optionsText = string.Join("\n", session.Options.Select(o => $"{o.Key}. {o.Text}"));

            var embedBuilder = new MessageEmbedBuilder();
            if (isPractice)
            {
                embedBuilder
                    .SetColor("#3B82F6") // Blue Accent for Practice Mode
                    .SetTitle("🎯 ĐỐ VUI TỪ VỰNG — CHẾ ĐỘ TỰ LUYỆN TẬP (DM)")
                    .SetDescription("⏱️ Thử sức nhanh với câu hỏi từ vựng trong 30 giây!\n💡 Lưu ý: Chế độ tự luyện tập trong tin nhắn riêng không tính điểm thưởng XP đua top Clan.")
                    .AddField("📝 Câu hỏi", session.QuestionPrompt, false)
                    .AddField("🎯 Các lựa chọn", optionsText, false)
                    .AddField("📊 Thông tin", $"Trình độ: {session.Level ?? "A1-A2"} • Độ khó: {session.Difficulty ?? 1}/5", false)
                    .SetFooter("Bấm nút bên dưới để kiểm tra kiến thức • LINGUAL Practice Mode", "");
            }
            else
            {
                embedBuilder
                    .SetColor("#F59E0B") // Amber / Golden
                    .SetTitle("🏆 ĐỐ VUI TỪ VỰNG CLAN")
                    .SetDescription("⚡ Nhanh tay chọn đáp án chính xác để ghi điểm cho Clan của bạn!\n⏱️ Thời gian trả lời: 30 giây\n🥇 Đúng nhanh nhất: +15 XP | Đúng tiếp theo: +5 XP")
                    .AddField("📝 Câu hỏi", session.QuestionPrompt, false)
                    .AddField("🎯 Các lựa chọn", optionsText, false)
                    .AddField("📊 Thông tin", $"Trình độ: {session.Level ?? "A1-A2"} • Độ khó: {session.Difficulty ?? 1}/5", false)
                    .SetFooter("Bấm nút bên dưới để chọn đáp án (mỗi bạn được chọn 1 lần) • LINGUAL Clan", "");
            }

            var buttonBuilder = new ButtonBuilder();
            foreach (var opt in session.Options)
            {
                var customId = $"btn_quiz_{session.SessionId}_{opt.Key}";
                var style = 2;
                buttonBuilder.AddButton(customId, $"{opt.Key}. {opt.Text}", style, false, "", "");
            }

            var introText = isPractice
                ? "📚 Câu đố luyện tập cá nhân của bạn:"
                : "🔥 Đố vui từ vựng vừa xuất hiện! Chọn đáp án ngay bên dưới:";

            var content = new MessageContentBuilder()
                .SetText(introText)
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttonBuilder.BuildComponents())
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);

            // Khởi chạy đồng hồ đếm ngược trong background để tự động tổng kết phiên nếu chưa hoàn thành
            var cts = new CancellationTokenSource();
            ActiveQuizTimers[session.SessionId] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cts.Token);
                    if (ActiveQuizTimers.TryRemove(session.SessionId, out _))
                    {
                        await ConcludeQuizAsync(channelId, session.SessionId);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Đã bị huỷ do người dùng trong DM trả lời xong ngay lập tức
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [MezonBot] Lỗi khi tự động kết thúc phiên Clan Quiz {SessionId}", session.SessionId);
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /quiz cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi bắt đầu câu đố. Vui lòng thử lại!");
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

            if (result.IsPractice)
            {
                var isUserCorrect = result.Participants.Any(p => p.IsCorrect);
                if (isUserCorrect)
                {
                    var userFirst = result.Participants.First(p => p.IsCorrect);
                    var sec = userFirst.ResponseMs / 1000.0;
                    embedBuilder
                        .SetColor("#10B981") // Emerald Green
                        .SetTitle("🎯 KẾT QUẢ TỰ LUYỆN TẬP — THÀNH CÔNG")
                        .SetDescription($"🎉 Bạn đã trả lời CHÍNH XÁC trong {sec:F2} giây! Rất xuất sắc.");
                }
                else
                {
                    embedBuilder
                        .SetColor("#64748B") // Slate Gray
                        .SetTitle("🎯 KẾT QUẢ TỰ LUYỆN TẬP — HẾT GIỜ")
                        .SetDescription("⏱️ Đã kết thúc 30 giây luyện tập! Hãy cùng xem lời giải chi tiết bên dưới:");
                }

                embedBuilder
                    .AddField("🎯 Đáp án chính xác", $"{result.CorrectKey}. {result.CorrectText}", false)
                    .AddField("📖 Giải thích chi tiết", result.Explanation ?? "Không có giải thích bổ sung.", false)
                    .SetFooter("Gõ /quiz trên kênh Chat Clan để thi đấu và nhận điểm thưởng XP nhé!", "");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(result.WinnerUsername))
                {
                    var seconds = (result.WinningResponseMs ?? 0) / 1000.0;
                    embedBuilder
                        .SetColor("#10B981") // Emerald Green - Vinh quang
                        .SetTitle("🏁 KẾT QUẢ ĐỐ VUI CLAN — BẢNG VÀNG")
                        .SetDescription($"🎉 Chúc mừng {result.WinnerUsername} đã xuất sắc trả lời đúng và nhanh nhất trong {seconds:F2} giây!")
                        .AddField("🥇 Người chiến thắng", $"{result.WinnerUsername} (+15 XP)", true)
                        .AddField("⏱️ Tốc độ phản xạ", $"{seconds:F2}s", true);
                }
                else
                {
                    embedBuilder
                        .SetColor("#64748B") // Slate Gray
                        .SetTitle("🏁 HẾT GIỜ ĐỐ VUI CLAN")
                        .SetDescription("⏱️ Đã hết 30 giây! Chưa có thành viên nào trả lời đúng câu hỏi này. Cùng ôn tập lại để bứt phá ở câu đố sau nhé!");
                }

                embedBuilder
                    .AddField("🎯 Đáp án chính xác", $"{result.CorrectKey}. {result.CorrectText}", false)
                    .AddField("📖 Giải thích chi tiết", result.Explanation ?? "Không có giải thích bổ sung.", false)
                    .AddField("📊 Thống kê phiên", $"Tổng người tham gia: {result.TotalResponses} | Trả lời đúng: {result.CorrectResponses} / {result.TotalResponses}", false)
                    .SetFooter("Gõ /quiz để bắt đầu câu đố mới!", "");
            }

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Ôn tập từ vựng 3D", 1, false, "", "")
                .BuildComponents();

            var introText = result.IsPractice
                ? "📢 Tổng kết phiên tự luyện tập:"
                : "📢 Tổng kết kết quả đố vui Clan:";

            var content = new MessageContentBuilder()
                .SetText(introText)
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
                    .SetTitle("🌟 LINGLING — ĐÁNH GIÁ NGỮ PHÁP")
                    .SetDescription("✨ Đánh giá: Rất chuẩn xác! • Bạn diễn đạt rất tự nhiên và chính xác!")
                    .AddField("📝 Câu của bạn", analysis.OriginalText, false)
                    .AddField("📖 Giải thích cấu trúc", analysis.Explanation, false)
                    .AddField("🎯 Mẹo mở rộng", analysis.Tips, false);
            }
            else
            {
                embedBuilder
                    .SetColor("#F59E0B") // Amber Orange - Cần chú ý / học hỏi
                    .SetTitle("🦉 LINGLING — GỢI Ý CHỈNH SỬA")
                    .SetDescription("💡 Đánh giá: Cần hoàn thiện • Xem câu gợi ý chuẩn tự nhiên bên dưới nhé:")
                    .AddField("📝 Câu gốc của bạn", analysis.OriginalText, false)
                    .AddField("✨ Gợi ý chuẩn bản xứ", analysis.CorrectedText, false)
                    .AddField("📖 Giải thích ngữ pháp", analysis.Explanation, false)
                    .AddField("🎯 Mẹo ghi nhớ", analysis.Tips, false);
            }

            embedBuilder.SetFooter("Powered by LINGUAL AI • Mezon Campus Studio 2026", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_save_flashcard", "💾 Lưu vào sổ tay", 1, false, "", "")
                .AddButton("btn_open_miniapp", "🚀 Ôn từ vựng 3D", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🦉 LingLing: Mình đã xem xong câu của bạn! Cùng xem phân tích chi tiết nhé:")
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
                await ReplyToChannelAsync(channelId, "💾 LingLing: Đã lưu mẫu câu và giải thích ngữ pháp này vào sổ tay học tập của bạn!");
            }
            else if (buttonId == "btn_open_miniapp")
            {
                await ReplyToChannelAsync(channelId, "🚀 LingLing: Đang mở không gian học từ vựng 3D... Chúc bạn có một buổi học thật hiệu quả!");
            }
            else if (buttonId == "btn_guide_ask")
            {
                await ReplyToChannelAsync(channelId, "🦉 LingLing: Bạn chỉ cần gõ /ask <câu tiếng Anh> (Ví dụ: /ask She don't like coffee) là mình sẽ sửa lỗi ngay!");
            }
            else if (buttonId == "btn_roleplay_menu")
            {
                var senderId = data.UserId.ToString();
                var sessionKey = GetRoleplaySessionKey(channelId, senderId);
                ActiveRoleplaySessions.TryRemove(sessionKey, out _);
                await HandleRoleplayCommandAsync(channelId, senderId, string.Empty);
            }
            else if (buttonId.StartsWith("btn_roleplay_select_"))
            {
                var scenarioCode = buttonId["btn_roleplay_select_".Length..];
                var senderId = data.UserId.ToString();
                await HandleRoleplayScenarioSelectedAsync(channelId, senderId, scenarioCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện click button");
        }
    }

    private async Task HandleRoleplayCommandAsync(long channelId, string senderId, string query)
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

            var scenarios = await aiTutor.GetAvailableScenariosAsync();

            if (string.IsNullOrWhiteSpace(query))
            {
                var embed = new MessageEmbedBuilder()
                    .SetColor("#8B5CF6")
                    .SetTitle("🎭 LINGLING ROLEPLAY — ĐÓNG VAI GIAO TIẾP TÌNH HUỐNG")
                    .SetDescription("Chọn một tình huống thực tế bên dưới để bắt đầu luyện giao tiếp tiếng Anh phản xạ cùng trợ lý LingLing:")
                    .AddField("☕ Quán Cà phê (A1-A2)", "Luyện gọi cà phê/trà, chọn size, loại sữa và thanh toán với Barista tại London.", false)
                    .AddField("✈️ Sân bay Quốc tế (A2-B1)", "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành tại Heathrow.", false)
                    .AddField("💼 Phỏng vấn xin việc (B1-B2)", "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh và định hướng.", false)
                    .SetFooter("Bấm nút bên dưới để chọn tình huống • LINGUAL Roleplay", "")
                    .Build();

                var buttons = new ButtonBuilder()
                    .AddButton("btn_roleplay_select_cafe_order", "☕ Quán Cà phê", 2, false, "", "")
                    .AddButton("btn_roleplay_select_airport_checkin", "✈️ Sân bay", 2, false, "", "")
                    .AddButton("btn_roleplay_select_job_interview", "💼 Phỏng vấn", 2, false, "", "")
                    .BuildComponents();

                var content = new MessageContentBuilder()
                    .SetText("🎭 Chọn tình huống đàm thoại thực tế:")
                    .AddEmbed(embed)
                    .AddActionRow(buttons)
                    .Build();

                await ReplyToChannelWithContentAsync(channelId, content);
                return;
            }

            var sessionKey = GetRoleplaySessionKey(channelId, senderId);
            ActiveRoleplaySessions.TryGetValue(sessionKey, out var activeSession);

            // Phân tích mã tình huống nếu người dùng gõ /roleplay <code/tên> <câu thoại>
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string scenarioCode;
            string userMessage;

            var matched = parts.Length >= 2
                ? scenarios.FirstOrDefault(s =>
                    s.Code.Equals(parts[0], StringComparison.OrdinalIgnoreCase) ||
                    s.Code.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase))
                : null;

            if (matched != null)
            {
                scenarioCode = matched.Code;
                userMessage = parts[1];
            }
            else if (activeSession != null && !string.IsNullOrWhiteSpace(activeSession.ScenarioCode))
            {
                scenarioCode = activeSession.ScenarioCode;
                userMessage = query;
            }
            else
            {
                scenarioCode = "cafe_order";
                userMessage = query;
            }

            Guid conversationId;
            if (activeSession != null && activeSession.ScenarioCode.Equals(scenarioCode, StringComparison.OrdinalIgnoreCase))
            {
                conversationId = activeSession.ConversationId;
                activeSession.LastActivity = DateTime.UtcNow;
            }
            else
            {
                conversationId = Guid.NewGuid();
                ActiveRoleplaySessions[sessionKey] = new UserRoleplaySession
                {
                    ConversationId = conversationId,
                    ScenarioCode = scenarioCode,
                    LastActivity = DateTime.UtcNow
                };
            }

            var result = await aiTutor.ProcessRoleplayTurnAsync(new RoleplayTurnRequest(
                ScenarioCode: scenarioCode,
                UserMessage: userMessage,
                ConversationId: conversationId,
                MezonUserId: senderId,
                ChannelId: channelId.ToString()
            ));

            var embedBuilder = new MessageEmbedBuilder()
                .SetColor("#10B981")
                .SetTitle($"🎭 {result.ScenarioTitle.ToUpper()}")
                .SetDescription($"🗣️ {result.CharacterRole}: \"{result.Reply}\"")
                .AddField("📝 Câu của bạn", userMessage, false);

            if (result.HasFeedback && !string.IsNullOrWhiteSpace(result.FeedbackCorrection))
            {
                embedBuilder.AddField("💡 Gợi ý chuẩn bản xứ", result.FeedbackCorrection, false);
                if (!string.IsNullOrWhiteSpace(result.Explanation))
                {
                    embedBuilder.AddField("📖 Giải thích", result.Explanation, false);
                }
            }

            embedBuilder.SetFooter($"Gõ /roleplay {scenarioCode} <câu tiếp theo> để trò chuyện tiếp nhé!", "");

            var buttonsRow = new ButtonBuilder()
                .AddButton("btn_save_flashcard", "💾 Lưu vào sổ tay", 1, false, "", "")
                .AddButton("btn_roleplay_menu", "🔄 Đổi tình huống", 2, false, "", "")
                .BuildComponents();

            var responseContent = new MessageContentBuilder()
                .SetText($"🦉 LingLing: Phản hồi tình huống từ {result.CharacterRole}:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttonsRow)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /roleplay cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Xin lỗi bạn, mình gặp chút gián đoạn khi kết nối AI. Bạn hãy thử lại sau ít giây nhé!");
        }
    }

    private async Task HandleRoleplayScenarioSelectedAsync(long channelId, string senderId, string scenarioCode)
    {
        try
        {
            var sessionKey = GetRoleplaySessionKey(channelId, senderId);
            var newConvId = Guid.NewGuid();
            ActiveRoleplaySessions[sessionKey] = new UserRoleplaySession
            {
                ConversationId = newConvId,
                ScenarioCode = scenarioCode,
                LastActivity = DateTime.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var aiTutor = scope.ServiceProvider.GetService<IAITutorService>();
            if (aiTutor == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ AI LingLing chưa sẵn sàng trên hệ thống.");
                return;
            }

            var scenarios = await aiTutor.GetAvailableScenariosAsync();
            var scenario = scenarios.FirstOrDefault(s => s.Code.Equals(scenarioCode, StringComparison.OrdinalIgnoreCase))
                ?? scenarios[0];

            var embed = new MessageEmbedBuilder()
                .SetColor("#8B5CF6")
                .SetTitle($"🎭 TÌNH HUỐNG: {scenario.Title.ToUpper()}")
                .SetDescription($"Trình độ gợi ý: {scenario.Level}\n\n🗣️ {scenario.CharacterRole}: \"{scenario.GreetingMessage}\"")
                .AddField("📝 Hướng dẫn đối thoại", $"Bạn hãy nhập câu trả lời bằng cách gõ lệnh:\n/roleplay {scenario.Code} <câu tiếng Anh của bạn>\n*Ví dụ:* /roleplay {scenario.Code} I would like a coffee, please", false)
                .SetFooter("LingLing sẽ đối đáp và sửa lỗi ngữ pháp giúp bạn • LINGUAL Roleplay", "")
                .Build();

            var content = new MessageContentBuilder()
                .SetText($"✨ Bạn đã vào tình huống {scenario.Title}:")
                .AddEmbed(embed)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi chọn kịch bản roleplay {ScenarioCode}", scenarioCode);
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
                if (result.IsPractice)
                {
                    // 1. Hủy bỏ ngay timer đếm ngược 30s trong DM vì người dùng đã có kết quả
                    if (ActiveQuizTimers.TryRemove(sessionId, out var cts))
                    {
                        try
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }
                        catch
                        {
                            // Bỏ qua lỗi hủy token nếu task đã kết thúc
                        }
                    }

                    // 2. Chốt phiên đố vui ngay lập tức và lấy đáp án / giải thích chi tiết
                    var conclude = await quizService.ConcludeSessionAsync(sessionId);
                    var seconds = result.ResponseMs / 1000.0;
                    var embedBuilder = new MessageEmbedBuilder();

                    if (result.IsCorrect)
                    {
                        embedBuilder
                            .SetColor("#10B981") // Emerald Green
                            .SetTitle("🎯 KẾT QUẢ TỰ LUYỆN TẬP — CHÍNH XÁC!")
                            .SetDescription($"🎉 Chúc mừng bạn đã trả lời **CHÍNH XÁC** trong **{seconds:F2}s**!\n💡 *Chế độ tự luyện tập DM: Không tính điểm thưởng XP và không đua top Clan.*");
                    }
                    else
                    {
                        embedBuilder
                            .SetColor("#EF4444") // Rose Red
                            .SetTitle("🎯 KẾT QUẢ TỰ LUYỆN TẬP — CHƯA CHÍNH XÁC")
                            .SetDescription($"❌ Bạn đã chọn đáp án **{selectedKey}** (chưa chính xác trong {seconds:F2}s).\n💡 *Chế độ tự luyện tập DM: Không bị trừ điểm XP.*");
                    }

                    if (conclude != null)
                    {
                        embedBuilder
                            .AddField("🎯 Đáp án chính xác", $"{conclude.CorrectKey}. {conclude.CorrectText}", false)
                            .AddField("📖 Giải thích chi tiết", conclude.Explanation ?? "Không có giải thích bổ sung.", false);
                    }

                    embedBuilder.SetFooter("Gõ /quiz để luyện tiếp câu mới • Ra kênh Clan để thi đấu tranh tài!", "");

                    var buttons = new ButtonBuilder()
                        .AddButton("btn_open_miniapp", "🚀 Ôn tập từ vựng 3D", 1, false, "", "")
                        .BuildComponents();

                    var intro = result.IsCorrect
                        ? "🌟 Kết quả câu hỏi tự luyện tập của bạn:"
                        : "💡 Đã có đáp án và giải thích câu hỏi luyện tập:";

                    var content = new MessageContentBuilder()
                        .SetText(intro)
                        .AddEmbed(embedBuilder.Build())
                        .AddActionRow(buttons)
                        .Build();

                    await ReplyToChannelWithContentAsync(channelId, content);
                }
                else
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
