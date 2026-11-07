using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
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
    private static readonly ConcurrentDictionary<string, DateTime> ButtonClickCooldowns = new();

    public record QuizMessageMeta(
        long ChannelId,
        long MessageId,
        string QuestionPrompt,
        string? Level,
        int? Difficulty,
        List<QuizOptionDto> Options,
        bool IsPractice
    );

    public static readonly ConcurrentDictionary<Guid, QuizMessageMeta> ActiveQuizMessages = new();

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

                var clientOptions = new MezonClientOptions(botId, _options.Token)
                {
                    EventDispatchMode = Mezon.Net.Client.EventDispatchMode.Concurrent
                };
                _client = new SdkClient(clientOptions);

                _client.Connected += OnConnectedAsync;
                _client.Disconnected += OnDisconnectedAsync;
                _client.Reconnecting += OnReconnectingAsync;
                _client.ChannelMessageReceived += OnChannelMessageReceivedAsync;
                _client.MessageButtonClicked += OnMessageButtonClickedAsync;
                _client.UserChannelAdded += OnUserChannelAddedAsync;

                await _client.LoginAsync();
                _logger.LogInformation("✅ [MezonBot] Đã đăng nhập thành công vào Gateway Mezon!");

                // Kích hoạt lắng nghe Realtime chat cho toàn bộ kênh DM hiện có của Bot
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    await EnsureDmChannelsJoinedAsync();
                });

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
                        _client.UserChannelAdded -= OnUserChannelAddedAsync;
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

    private async Task EnsureDmChannelsJoinedAsync()
    {
        if (_client == null) return;
        try
        {
            var engineProp = typeof(SdkClient).GetProperty("Engine", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var engine = engineProp?.GetValue(_client) as Mezon.Net.Client.MezonClient;
            if (engine == null) return;

            var joinedCount = 0;

            // 1. Quét từ DmChannelManager của SDK
            var dmProp = typeof(SdkClient).GetProperty("DmChannels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var dmManager = dmProp?.GetValue(_client);
            if (dmManager != null)
            {
                var dmListProp = dmManager.GetType().GetProperty("DmChannelDescriptions");
                var dmList = dmListProp?.GetValue(dmManager) as System.Collections.IEnumerable;
                if (dmList != null)
                {
                    foreach (var dmObj in dmList)
                    {
                        var chIdProp = dmObj.GetType().GetProperty("ChannelId");
                        var typeProp = dmObj.GetType().GetProperty("Type");
                        var clanIdProp = dmObj.GetType().GetProperty("ClanId");
                        var privProp = dmObj.GetType().GetProperty("ChannelPrivate");

                        var chId = (long)(chIdProp?.GetValue(dmObj) ?? 0L);
                        var chType = (int)(typeProp?.GetValue(dmObj) ?? 0);
                        var cId = (long)(clanIdProp?.GetValue(dmObj) ?? 0L);
                        var priv = (int)(privProp?.GetValue(dmObj) ?? 0);

                        if (chId > 0)
                        {
                            await engine.JoinChannelChatRtAsync(new ChannelJoinParams(
                                clanId: cId,
                                channelId: chId,
                                channelType: chType,
                                isPublic: priv == 0 && cId > 0
                            ));
                            joinedCount++;
                            _logger.LogInformation("🔗 [MezonBot] Đã kết nối lắng nghe kênh DM {ChannelId} thành công!", chId);
                        }
                    }
                }
            }

            // 2. Dự phòng: Quét từ ListChannelByUserIdAsync
            try
            {
                var userChannels = await engine.ListChannelByUserIdAsync();
                if (userChannels.Channeldesc.Count > 0)
                {
                    foreach (var ch in userChannels.Channeldesc)
                    {
                        if (ch.ClanId == 0 || ch.ChannelPrivate > 0)
                        {
                            await engine.JoinChannelChatRtAsync(new ChannelJoinParams(
                                clanId: ch.ClanId,
                                channelId: ch.ChannelId,
                                channelType: ch.Type,
                                isPublic: ch.ChannelPrivate == 0 && ch.ClanId > 0
                            ));
                            joinedCount++;
                            _logger.LogInformation("🔗 [MezonBot] Đã kết nối lắng nghe kênh DM/Private {ChannelId} thành công!", ch.ChannelId);
                        }
                    }
                }
            }
            catch (Exception exUser)
            {
                _logger.LogDebug(exUser, "ListChannelByUserIdAsync quét hoàn tất.");
            }

            _logger.LogInformation("✅ [MezonBot] Tổng cộng đã kích hoạt lắng nghe {Count} kênh DM/Private.", joinedCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi quét danh sách kênh DM lúc khởi động");
        }
    }

    private async Task OnUserChannelAddedAsync(UserChannelAddedEventData eventData)
    {
        try
        {
            UserChannelAddedResponse response = (UserChannelAddedResponse)eventData;
            var ch = response.ChannelDesc;
            if (ch.ClanId == 0 || ch.ChannelPrivate > 0)
            {
                var engineProp = typeof(SdkClient).GetProperty("Engine", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var engine = engineProp?.GetValue(_client) as Mezon.Net.Client.MezonClient;
                if (engine != null)
                {
                    await engine.JoinChannelChatRtAsync(new ChannelJoinParams(
                        clanId: ch.ClanId,
                        channelId: ch.ChannelId,
                        channelType: ch.Type,
                        isPublic: ch.ChannelPrivate == 0 && ch.ClanId > 0
                    ));
                    _logger.LogInformation("🔗 [MezonBot] Đã tự động kết nối lắng nghe kênh DM mới {ChannelId}!", ch.ChannelId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi xử lý sự kiện UserChannelAdded");
        }
    }

    private async Task OnChannelMessageReceivedAsync(ChannelMessageEventData eventData)
    {
        try
        {
            ChannelMessageResponse data = (ChannelMessageResponse)eventData;
            _logger.LogInformation("📩 [MezonBot RAW] Channel: {ChannelId}, Clan: {ClanId}, Public: {IsPublic}, Sender: {SenderId}",
                data.ChannelId, data.ClanId, data.IsPublic, data.SenderId);

            var rawContent = data.Content;
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return;
            }

            var parsed = MessageContent.Parse(rawContent);
            var text = parsed.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(rawContent))
            {
                text = rawContent.Trim();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _logger.LogInformation("📩 [MezonBot] Nhận tin nhắn từ Channel {ChannelId} (Clan {ClanId}): '{Text}'", data.ChannelId, data.ClanId, text);
            var isDm = data.ClanId <= 0;

            if (text.Equals("/ping", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyToChannelAsync(data.ChannelId, "🏓 Pong! Bot LINGUAL hoạt động bình thường trên nền tảng Mezon Campus Studio 2026. (SDK: Mezon.Net.Sdk 1.7.0)", data);
            }
            else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                await HandleHelpCommandAsync(data.ChannelId, data);
            }
            else if (text.Equals("/learn", StringComparison.OrdinalIgnoreCase))
            {
                await HandleLearnCommandAsync(data.ChannelId, data);
            }
            else if (text.Equals("/daily", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/streak", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/checkin", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                var displayName = string.IsNullOrWhiteSpace(data.Username)
                    ? $"Member_{senderId[..Math.Min(6, senderId.Length)]}"
                    : data.Username;
                await HandleDailyCommandAsync(data.ChannelId, senderId, displayName, data);
            }
            else if (text.Equals("/quiz", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                await HandleQuizCommandAsync(data.ChannelId, senderId, isDm, data);
            }
            else if (text.StartsWith("/roleplay", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/scenario", StringComparison.OrdinalIgnoreCase))
            {
                var prefixLength = text.StartsWith("/roleplay", StringComparison.OrdinalIgnoreCase) ? 9 : 9;
                var query = text[prefixLength..].Trim();
                var senderId = data.SenderId.ToString();
                await HandleRoleplayCommandAsync(data.ChannelId, senderId, query, data);
            }
            else if (text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/check", StringComparison.OrdinalIgnoreCase))
            {
                var prefixLength = text.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ? 4 : 6;
                var query = text[prefixLength..].Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    await ReplyToChannelAsync(data.ChannelId, "🦉 LingLing: Bạn hãy nhập câu tiếng Anh cần kiểm tra sau lệnh /ask nhé!\nVí dụ: /ask I go to school yesterday", data);
                    return;
                }

                await HandleAskGrammarAsync(data.ChannelId, query, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý tin nhắn sự kiện");
        }
    }

    private async Task HandleHelpCommandAsync(long channelId, ChannelMessageResponse? replyTo = null)
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#3B82F6") // Blue Accent
            .SetTitle("🤖 LINGUAL BOT — TRỢ LÝ HỌC TIẾNG ANH")
            .SetDescription("Chào mừng bạn đến với LINGUAL trên nền tảng Mezon Campus Studio 2026!")
            .AddField("🔥 Duy trì Streak hàng ngày", "• /daily: Điểm danh nhận +20 XP và duy trì ngọn lửa Streak mỗi ngày\n• /streak: Tra cứu chuỗi ngày học và kỷ lục của bạn", false)
            .AddField("📚 Học tập & Luyện ngữ pháp", "• /ask <câu>: Trợ lý LingLing phân tích & sửa lỗi ngữ pháp\n• /roleplay: Đóng vai luyện giao tiếp tiếng Anh theo tình huống thực tế\n• /learn: Ôn tập từ vựng qua thẻ ghi nhớ 3D thông minh", false)
            .AddField("🏆 Đấu trường Clan", "• /quiz: Tham gia đố vui từ vựng 30 giây tranh tài cùng Clan (hoặc luyện tập cá nhân trong DM)", false)
            .AddField("🌐 Webview Miniapp Học tập", $"• AI Tutor LingLing: {_options.WebAppUrl}/tutor\n• Luyện tập Quiz: {_options.WebAppUrl}/quiz\n• Bảng xếp hạng: {_options.WebAppUrl}/leaderboard\n• Thẻ ghi nhớ 3D: {_options.WebAppUrl}/learn", false)
            .AddField("⚡ Tiện ích & Trạng thái", "• /ping: Kiểm tra kết nối của Bot\n• /help: Xem danh sách lệnh trợ giúp", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_daily_checkin", "🔥 Điểm danh /daily", 1, false, "", "")
            .AddButton("btn_roleplay_menu", "🎭 Luyện Roleplay", 2, false, "", "")
            .AddButton("btn_open_miniapp", "🚀 Học từ vựng", 2, false, "", "")
            .AddButton("btn_guide_ask", "🦉 Hướng dẫn /ask", 2, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("👋 Danh sách các tính năng học tập:")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content, replyTo);
    }

    private async Task HandleLearnCommandAsync(long channelId, ChannelMessageResponse? replyTo = null)
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#6366F1") // Indigo Accent
            .SetTitle("📚 HỌC TỪ VỰNG TIẾNG ANH CÙNG LINGUAL")
            .SetDescription("Ôn tập từ vựng qua thẻ ghi nhớ 3D thông minh, giúp bạn nhớ sâu và lâu quên hơn mỗi ngày!")
            .AddField("💡 Mẹo nhỏ", "Gõ /ask <câu tiếng Anh> bất kỳ lúc nào để nhờ trợ lý LingLing giải thích ngữ pháp hoặc từ mới.", false)
            .AddField("⚔️ Đấu trường Clan", "Gõ /quiz để tham gia đố vui từ vựng cùng Clan hoặc luyện tập trong DM!", false)
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

        await ReplyToChannelWithContentAsync(channelId, content, replyTo);
    }

    private async Task HandleDailyCommandAsync(
        long channelId,
        string senderId,
        string displayName,
        ChannelMessageResponse? replyTo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var streakService = scope.ServiceProvider.GetRequiredService<IUserStreakService>();

            var result = await streakService.ProcessDailyCheckinAsync(senderId, displayName);

            var embedBuilder = new MessageEmbedBuilder();

            if (result.AlreadyCheckedInToday)
            {
                embedBuilder
                    .SetColor("#3B82F6") // Blue Accent
                    .SetTitle("🔥 ĐIỂM DANH HÀNG NGÀY — BẠN ĐÃ ĐIỂM DANH HÔM NAY!")
                    .SetDescription($"Chào **{displayName}**, bạn đã hoàn thành điểm danh bảo lưu streak cho ngày hôm nay rồi!\nHãy tiếp tục giữ vững phong độ học tập nhé.")
                    .AddField("🔥 Chuỗi hiện tại", $"**{result.CurrentDays}** ngày liên tiếp", true)
                    .AddField("🏆 Kỷ lục chuỗi", $"**{result.BestDays}** ngày", true)
                    .AddField("❄️ Khiên Streak Freeze", $"**{result.FreezeBalance}** khiên bảo vệ", true)
                    .AddField("💡 Mẹo nhỏ", "Gõ /quiz để thi đấu đố vui từ vựng cùng Clan hoặc tự luyện trong DM!", false)
                    .SetFooter("LINGUAL Streak System • Mezon Campus Studio 2026", "");
            }
            else
            {
                var freezeNotice = result.FreezeUsed
                    ? "🛡️ *Chuỗi ngày của bạn vừa được bảo vệ thành công bởi 1 Khiên Streak Freeze!*"
                    : (result.CurrentDays > 1
                        ? "🎉 *Xuất sắc! Chuỗi ngày học liên tiếp của bạn đã được nối dài!*"
                        : "🌱 *Bắt đầu chuỗi học tập mới! Hãy duy trì mỗi ngày để tích lũy XP nhé!*");

                embedBuilder
                    .SetColor("#F59E0B") // Amber Flame
                    .SetTitle("🔥 ĐIỂM DANH THÀNH CÔNG — GIỮ VỮNG STREAK!")
                    .SetDescription($"Chúc mừng **{displayName}** đã điểm danh thành công!\n{freezeNotice}")
                    .AddField("🔥 Chuỗi hiện tại", $"**{result.CurrentDays}** ngày liên tiếp", true)
                    .AddField("🏆 Kỷ lục chuỗi", $"**{result.BestDays}** ngày", true)
                    .AddField("⚡ Thưởng hôm nay", $"+{result.XpAwarded} XP", true)
                    .AddField("❄️ Khiên Streak Freeze", $"**{result.FreezeBalance}** khiên", true)
                    .AddField("🎯 Hoạt động đề xuất", "• Gõ /quiz để tranh tài từ vựng 30 giây cùng Clan\n• Gõ /roleplay để luyện giao tiếp tiếng Anh theo tình huống", false)
                    .SetFooter("Duy trì streak mỗi ngày để mở khóa huy hiệu và leo hạng Clan! • LINGUAL", "");
            }

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Bắt đầu học ngay", 1, false, "", "")
                .AddButton("btn_quiz_start_prompt", "⚔️ Thử thách Quiz", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText(result.AlreadyCheckedInToday ? "✨ Trạng thái chuỗi học tập Streak của bạn:" : "🌟 Điểm danh thành công! Cùng duy trì ngọn lửa học tập mỗi ngày:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            await ReplyToChannelWithContentAsync(channelId, content, replyTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /daily cho User {UserId}", senderId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi thực hiện điểm danh. Vui lòng thử lại sau!", replyTo);
        }
    }

    private async Task HandleQuizCommandAsync(
        long channelId,
        string? senderId,
        bool isPractice = false,
        ChannelMessageResponse? replyTo = null)
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
                await ReplyToChannelAsync(channelId, busyMsg, replyTo);
                return;
            }

            const int durationSeconds = 30;
            var session = await quizService.StartSessionAsync(channelId, senderId, durationSeconds, isPractice);

            if (session == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Không thể khởi tạo câu đố vào lúc này. Vui lòng thử lại sau ít phút!", replyTo);
                return;
            }

            var optionsText = string.Join("\n", session.Options.Select(o => $"{o.Key}. {o.Text}"));

            var title = isPractice ? "📖 TỰ LUYỆN TẬP TỪ VỰNG (DM)" : "🏆 ĐỐ VUI TỪ VỰNG CLAN";
            var desc = isPractice
                ? "🎯 Chế độ luyện tập cá nhân: Chọn đáp án đúng bên dưới để nhận phản hồi và giải thích chi tiết ngay lập tức!\n💡 Không áp lực thời gian, không tranh hạng Clan."
                : "⚡ Nhanh tay chọn đáp án chính xác để ghi điểm cho Clan của bạn!\n⏱️ Thời gian trả lời: 30 giây\n🥇 Đúng nhanh nhất: +15 XP | Đúng tiếp theo: +5 XP";

            var embed = new MessageEmbedBuilder()
                .SetColor(isPractice ? "#6366F1" : "#F59E0B")
                .SetTitle(title)
                .SetDescription(desc)
                .AddField("📝 Câu hỏi", session.QuestionPrompt, false)
                .AddField("🎯 Các lựa chọn", optionsText, false)
                .AddField("📊 Thông tin", $"Trình độ: {session.Level ?? "A1-A2"} • Độ khó: {session.Difficulty ?? 1}/5", false)
                .SetFooter("Bấm nút bên dưới để chọn đáp án (mỗi bạn được chọn 1 lần) • LINGUAL Quiz", "")
                .Build();

            var buttonBuilder = new ButtonBuilder();
            foreach (var opt in session.Options)
            {
                var customId = $"btn_quiz_{session.SessionId}_{opt.Key}";
                buttonBuilder.AddButton(customId, $"{opt.Key}. {opt.Text}", 2, false, "", "");
            }

            var content = new MessageContentBuilder()
                .SetText(isPractice ? "📚 Câu hỏi luyện tập của bạn đã sẵn sàng:" : "🔥 Đố vui từ vựng vừa xuất hiện! Chọn đáp án ngay bên dưới:")
                .AddEmbed(embed)
                .AddActionRow(buttonBuilder.BuildComponents())
                .Build();

            var sentMessageId = await ReplyToChannelWithContentAsync(channelId, content, replyTo);
            if (sentMessageId > 0)
            {
                ActiveQuizMessages[session.SessionId] = new QuizMessageMeta(
                    channelId,
                    sentMessageId,
                    session.QuestionPrompt,
                    session.Level,
                    session.Difficulty,
                    session.Options,
                    isPractice
                );
            }

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
                    // Đã được chốt sớm do người dùng trong DM trả lời xong ngay lập tức
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
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi bắt đầu câu đố. Vui lòng thử lại!", replyTo);
        }
    }

    private async Task ConcludeQuizAsync(long channelId, Guid sessionId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            var result = await quizService.ConcludeSessionAsync(sessionId);
            if (result == null) return;

            // Vô hiệu hóa toàn bộ nút trên tin nhắn câu hỏi gốc để ngăn chặn spam
            if (ActiveQuizMessages.TryRemove(sessionId, out var quizMeta))
            {
                await DisableQuizMessageButtonsAsync(channelId, quizMeta.MessageId, quizMeta);
            }

            var embedBuilder = new MessageEmbedBuilder();

            if (result.IsPractice)
            {
                embedBuilder
                    .SetColor("#6366F1")
                    .SetTitle("🏁 KẾT THÚC CÂU HỎI LUYỆN TẬP")
                    .SetDescription("💡 Hết thời gian câu hỏi tự luyện tập cá nhân. Cùng xem đáp án chi tiết bên dưới nhé:");
            }
            else if (!string.IsNullOrWhiteSpace(result.WinnerUsername))
            {
                var seconds = (result.WinningResponseMs ?? 0) / 1000.0;
                embedBuilder
                    .SetColor("#10B981")
                    .SetTitle("🏁 KẾT QUẢ ĐỐ VUI CLAN — BẢNG VÀNG")
                    .SetDescription($"🎉 Chúc mừng {result.WinnerUsername} đã xuất sắc trả lời đúng và nhanh nhất trong {seconds:F2} giây!")
                    .AddField("🥇 Người chiến thắng", $"{result.WinnerUsername} (+15 XP)", true)
                    .AddField("⏱️ Tốc độ phản xạ", $"{seconds:F2}s", true);
            }
            else
            {
                embedBuilder
                    .SetColor("#64748B")
                    .SetTitle("🏁 HẾT GIỜ ĐỐ VUI CLAN")
                    .SetDescription("⏱️ Đã hết 30 giây! Chưa có thành viên nào trả lời đúng câu hỏi này. Cùng ôn tập lại để bứt phá ở câu đố sau nhé!");
            }

            embedBuilder
                .AddField("🎯 Đáp án chính xác", $"{result.CorrectKey}. {result.CorrectText}", false)
                .AddField("📖 Giải thích chi tiết", result.Explanation ?? "Không có giải thích bổ sung.", false)
                .AddField("📊 Thống kê phiên", $"Tổng người tham gia: {result.TotalResponses} | Trả lời đúng: {result.CorrectResponses} / {result.TotalResponses}", false)
                .SetFooter("Gõ /quiz để bắt đầu câu đố mới!", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_open_miniapp", "🚀 Ôn tập từ vựng 3D", 1, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText(result.IsPractice ? "📖 Đáp án câu hỏi luyện tập:" : "📢 Tổng kết kết quả đố vui Clan:")
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

    private async Task HandleAskGrammarAsync(long channelId, string userQuery, ChannelMessageResponse? replyTo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aiTutor = scope.ServiceProvider.GetService<IAITutorService>();

            if (aiTutor == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ AI LingLing chưa sẵn sàng trên hệ thống.", replyTo);
                return;
            }

            var analysis = await aiTutor.AnalyzeGrammarAsync(new GrammarAnalysisRequest(userQuery));

            var embedBuilder = new MessageEmbedBuilder();

            if (analysis.IsCorrect)
            {
                embedBuilder
                    .SetColor("#10B981")
                    .SetTitle("🌟 LINGLING — ĐÁNH GIÁ NGỮ PHÁP")
                    .SetDescription("✨ Đánh giá: Rất chuẩn xác! • Bạn diễn đạt rất tự nhiên và chính xác!")
                    .AddField("📝 Câu của bạn", analysis.OriginalText, false)
                    .AddField("📖 Giải thích cấu trúc", analysis.Explanation, false)
                    .AddField("🎯 Mẹo mở rộng", analysis.Tips, false);
            }
            else
            {
                embedBuilder
                    .SetColor("#F59E0B")
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

            await ReplyToChannelWithContentAsync(channelId, content, replyTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /ask cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Xin lỗi bạn, mình đang gặp chút gián đoạn khi kết nối AI. Bạn hãy thử lại sau ít giây nhé!", replyTo);
        }
    }

    private async Task OnMessageButtonClickedAsync(MessageButtonClickedEventData eventData)
    {
        try
        {
            MessageButtonClickedResponse data = (MessageButtonClickedResponse)eventData;
            var buttonId = data.ButtonId;
            var channelId = data.ChannelId;
            var mezonUserId = data.UserId.ToString();

            _logger.LogInformation("🔘 [MezonBot] Người dùng {UserId} bấm nút {ButtonId} tại Channel {ChannelId}", mezonUserId, buttonId, channelId);

            // Kiểm soát chống spam nút bấm (Debounce 2 giây cho cùng người dùng và nút)
            var cooldownKey = $"{channelId}_{mezonUserId}_{buttonId}";
            var now = DateTime.UtcNow;
            if (ButtonClickCooldowns.TryGetValue(cooldownKey, out var lastClick) && (now - lastClick).TotalSeconds < 2)
            {
                _logger.LogWarning("⚠️ [MezonBot] Bỏ qua spam click từ User {UserId} cho nút {ButtonId}", mezonUserId, buttonId);
                return;
            }
            ButtonClickCooldowns[cooldownKey] = now;

            if (buttonId.StartsWith("btn_quiz_disabled_"))
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, "🔒 Câu hỏi này đã kết thúc hoặc bạn đã chọn đáp án trước đó rồi nhé!");
                }
                return;
            }
            else if (buttonId.StartsWith("btn_quiz_"))
            {
                // Format: btn_quiz_{sessionId}_{key}
                var parts = buttonId.Split('_');
                if (parts.Length >= 4 && Guid.TryParse(parts[2], out var sessionId))
                {
                    var selectedKey = parts[3];
                    var username = $"Member_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";

                    await HandleQuizButtonClickAsync(channelId, sessionId, mezonUserId, username, selectedKey, data.MessageId);
                }
            }
            else if (buttonId == "btn_daily_checkin")
            {
                var username = $"Member_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";
                await HandleDailyCommandAsync(channelId, mezonUserId, username);
            }
            else if (buttonId == "btn_quiz_start_prompt")
            {
                var isDm = channelId > 0 && channelId == data.ChannelId;
                await HandleQuizCommandAsync(channelId, mezonUserId, isDm);
            }
            else if (buttonId == "btn_save_flashcard")
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, "💾 LingLing: Đã lưu mẫu câu và giải thích ngữ pháp này vào sổ tay học tập của bạn!");
                }
            }
            else if (buttonId == "btn_open_miniapp")
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, $"🚀 LingLing: Mời bạn mở Webview học tập tại: {_options.WebAppUrl}/quiz (Luyện tập Quiz) hoặc {_options.WebAppUrl}/tutor (Luyện giao tiếp AI)!");
                }
            }
            else if (buttonId == "btn_guide_ask")
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, "🦉 LingLing: Bạn chỉ cần gõ /ask <câu tiếng Anh> (Ví dụ: /ask She don't like coffee) là mình sẽ sửa lỗi ngay!");
                }
            }
            else if (buttonId == "btn_roleplay_menu")
            {
                var sessionKey = GetRoleplaySessionKey(channelId, mezonUserId);
                ActiveRoleplaySessions.TryRemove(sessionKey, out _);
                await HandleRoleplayCommandAsync(channelId, mezonUserId, string.Empty);
            }
            else if (buttonId.StartsWith("btn_roleplay_select_"))
            {
                var scenarioCode = buttonId["btn_roleplay_select_".Length..];
                await HandleRoleplayScenarioSelectedAsync(channelId, mezonUserId, scenarioCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện click button");
        }
    }

    private async Task HandleRoleplayCommandAsync(
        long channelId,
        string senderId,
        string query,
        ChannelMessageResponse? replyTo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aiTutor = scope.ServiceProvider.GetService<IAITutorService>();
            if (aiTutor == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ AI LingLing chưa sẵn sàng trên hệ thống.", replyTo);
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

                await ReplyToChannelWithContentAsync(channelId, content, replyTo);
                return;
            }

            var sessionKey = GetRoleplaySessionKey(channelId, senderId);
            ActiveRoleplaySessions.TryGetValue(sessionKey, out var activeSession);

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

            await ReplyToChannelWithContentAsync(channelId, responseContent, replyTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /roleplay cho channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "🦉 LingLing: Xin lỗi bạn, mình gặp chút gián đoạn khi kết nối AI. Bạn hãy thử lại sau ít giây nhé!", replyTo);
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
        string selectedKey,
        long clickedMessageId = 0)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quizService = scope.ServiceProvider.GetRequiredService<IClanQuizService>();

            var result = await quizService.SubmitAnswerAsync(sessionId, channelId, mezonUserId, username, selectedKey);

            if (result.Status == QuizSubmissionStatus.AlreadyAnswered)
            {
                // Tránh spam tin nhắn nếu người dùng bấm nhiều lần
                _logger.LogInformation("ℹ️ [MezonBot] User {UserId} đã gửi câu trả lời rồi. Gửi Ephemeral thông báo.", mezonUserId);
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, "⚠️ Bạn đã gửi câu trả lời cho câu hỏi này rồi nhé!");
                }
                return;
            }
            else if (result.Status == QuizSubmissionStatus.SessionExpired)
            {
                _logger.LogInformation("ℹ️ [MezonBot] Phiên đố vui {SessionId} đã hết hạn. Gửi Ephemeral thông báo tới User {UserId}.", sessionId, mezonUserId);
                if (long.TryParse(mezonUserId, out var uid))
                {
                    await SendEphemeralMessageAsync(channelId, uid, "⏰ Phiên đố vui này đã kết thúc nhận đáp án!");
                }
                if (ActiveQuizMessages.TryGetValue(sessionId, out var expiredMeta))
                {
                    var mId = clickedMessageId > 0 ? clickedMessageId : expiredMeta.MessageId;
                    await DisableQuizMessageButtonsAsync(channelId, mId, expiredMeta);
                }
                return;
            }
            else if (result.Status == QuizSubmissionStatus.Success)
            {
                if (result.IsPractice)
                {
                    // 1. Huỷ ngay lập tức bộ đếm thời gian 30s
                    if (ActiveQuizTimers.TryRemove(sessionId, out var cts))
                    {
                        try
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }
                        catch { }
                    }

                    // 2. Chốt phiên đố vui ngay lập tức và lấy đáp án / giải thích chi tiết
                    var conclude = await quizService.ConcludeSessionAsync(sessionId);
                    var seconds = result.ResponseMs / 1000.0;
                    var embedBuilder = new MessageEmbedBuilder();

                    if (result.IsCorrect)
                    {
                        embedBuilder
                            .SetColor("#10B981")
                            .SetTitle("🎯 KẾT QUẢ TỰ LUYỆN TẬP — CHÍNH XÁC!")
                            .SetDescription($"🎉 Chúc mừng bạn đã trả lời **CHÍNH XÁC** trong **{seconds:F2}s**!\n💡 *Chế độ tự luyện tập DM: Không tính điểm thưởng XP và không đua top Clan.*");
                    }
                    else
                    {
                        embedBuilder
                            .SetColor("#EF4444")
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

                    // 3. Vô hiệu hóa toàn bộ nút chọn đáp án trên tin nhắn câu hỏi gốc để tránh spam
                    if (ActiveQuizMessages.TryRemove(sessionId, out var quizMeta))
                    {
                        var targetMsgId = clickedMessageId > 0 ? clickedMessageId : quizMeta.MessageId;
                        await DisableQuizMessageButtonsAsync(channelId, targetMsgId, quizMeta, selectedKey);
                    }
                }
                else
                {
                    // Clan Quiz mode
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

    private async Task DisableQuizMessageButtonsAsync(
        long channelId,
        long messageId,
        QuizMessageMeta meta,
        string? selectedKey = null)
    {
        if (_client == null || channelId <= 0 || messageId <= 0) return;

        try
        {
            var title = meta.IsPractice ? "📖 TỰ LUYỆN TẬP TỪ VỰNG (DM)" : "🏆 ĐỐ VUI TỪ VỰNG CLAN";
            var desc = meta.IsPractice
                ? "✅ Bạn đã hoàn thành câu hỏi luyện tập này."
                : "🔒 Phiên đố vui này đã kết thúc nhận đáp án.";

            var optionsText = string.Join("\n", meta.Options.Select(o =>
            {
                var isChosen = !string.IsNullOrEmpty(selectedKey) && o.Key.Equals(selectedKey, StringComparison.OrdinalIgnoreCase);
                return isChosen ? $"👉 **{o.Key}. {o.Text}** *(Đã chọn)*" : $"{o.Key}. {o.Text}";
            }));

            var embed = new MessageEmbedBuilder()
                .SetColor("#6B7280")
                .SetTitle(title)
                .SetDescription(desc)
                .AddField("📝 Câu hỏi", meta.QuestionPrompt, false)
                .AddField("🎯 Các lựa chọn", optionsText, false)
                .AddField("📊 Thông tin", $"Trình độ: {meta.Level ?? "A1-A2"} • Độ khó: {meta.Difficulty ?? 1}/5", false)
                .SetFooter("Phiên câu hỏi đã hoàn tất • LINGUAL Quiz", "")
                .Build();

            var disabledButtons = new ButtonBuilder();
            foreach (var opt in meta.Options)
            {
                var isChosen = !string.IsNullOrEmpty(selectedKey) && opt.Key.Equals(selectedKey, StringComparison.OrdinalIgnoreCase);
                var style = isChosen ? 1 : 2;
                var label = isChosen ? $"✓ {opt.Key}. {opt.Text}" : $"{opt.Key}. {opt.Text}";
                disabledButtons.AddButton($"btn_quiz_disabled_{opt.Key}", label, style, disable: true, "", "");
            }

            var content = new MessageContentBuilder()
                .SetText(meta.IsPractice ? "📚 Câu hỏi tự luyện tập (Đã kết thúc):" : "🏁 Phiên đố vui Clan (Đã hoàn thành):")
                .AddEmbed(embed)
                .AddActionRow(disabledButtons.BuildComponents())
                .Build();

            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.UpdateMessageAsync(messageId, content);
                _logger.LogInformation("🔒 [MezonBot] Đã disable toàn bộ nút cho Quiz Message {MessageId} tại Channel {ChannelId}", messageId, channelId);
            }
            else
            {
                var updateParams = new ChannelMessageUpdateParams(
                    clanId: 0,
                    channelId: channelId,
                    messageId: messageId,
                    content: content.ToJson(),
                    mentions: null,
                    attachments: null,
                    mode: 0,
                    isPublic: false,
                    hideEditted: false,
                    topicId: null,
                    isUpdateMsgTopic: null,
                    createTimeSeconds: null
                );
                await _client.UpdateChannelMessageAsync(updateParams);
                _logger.LogInformation("🔒 [MezonBot] Đã disable toàn bộ nút (Fallback) cho Quiz Message {MessageId} tại Channel {ChannelId}", messageId, channelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi cập nhật disable buttons cho Quiz Message {MessageId}", messageId);
        }
    }

    private async Task<long> ReplyToChannelWithContentAsync(
        long channelId,
        MessageContent content,
        ChannelMessageResponse? replyTo = null)
    {
        if (_client == null || channelId <= 0) return 0;

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            IEnumerable<MessageRefParams>? refs = null;

            if (replyTo != null && replyTo.Value.MessageId > 0)
            {
                refs = new[]
                {
                    new MessageRefParams(
                        messageId: replyTo.Value.MessageId,
                        messageRefId: replyTo.Value.MessageId,
                        content: replyTo.Value.Content,
                        hasAttachment: false,
                        refType: 0,
                        messageSenderId: replyTo.Value.SenderId,
                        messageSenderUsername: replyTo.Value.Username,
                        messageSenderAvatar: replyTo.Value.Avatar,
                        messageSenderClanNick: string.Empty,
                        messageSenderDisplayName: replyTo.Value.Username
                    )
                };
            }

            if (channel != null)
            {
                ChannelMessageAckResponse? ack;
                if (refs != null)
                {
                    ack = await channel.SendAsync(content, references: refs);
                }
                else
                {
                    ack = await channel.SendAsync(content);
                }
                _logger.LogInformation("📤 [MezonBot] Đã gửi phản hồi đến Channel {ChannelId} (MessageId: {MessageId})", channelId, ack?.MessageId);
                return ack?.MessageId ?? 0;
            }
            else
            {
                _logger.LogWarning("⚠️ [MezonBot] Kênh {ChannelId} là DM/không có trong cache. Sử dụng fallback SendChannelMessageAsync...", channelId);
                var sendParams = new SendChannelMessageParams(
                    clanId: 0,
                    channelId: channelId,
                    content: content.ToJson(),
                    topicId: null,
                    isPublic: false,
                    mode: 0,
                    code: 0,
                    mentionEveryone: false,
                    anonymousMessage: false,
                    mentions: null,
                    attachments: null,
                    references: refs
                );
                var ack = await _client.SendChannelMessageAsync(sendParams);
                _logger.LogInformation("📤 [MezonBot] Đã gửi phản hồi FALLBACK đến Channel DM {ChannelId} (MessageId: {MessageId})", channelId, ack.MessageId);
                return ack.MessageId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi tin nhắn tới channel {ChannelId}", channelId);
            return 0;
        }
    }

    private async Task ReplyToChannelAsync(
        long channelId,
        string responseText,
        ChannelMessageResponse? replyTo = null)
    {
        if (_client == null || channelId <= 0) return;

        try
        {
            var content = new MessageContentBuilder().SetText(responseText).Build();
            await ReplyToChannelWithContentAsync(channelId, content, replyTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Không thể gửi tin nhắn phản hồi tới channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    /// Gửi tin nhắn Ephemeral (chỉ duy nhất người dùng nhận được chỉ định nhìn thấy).
    /// </summary>
    public async Task SendEphemeralMessageAsync(long channelId, long userId, string text)
    {
        if (_client == null || channelId <= 0 || userId <= 0) return;

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendEphemeralTextAsync(text, userId);
                _logger.LogInformation("🤫 [MezonBot] Đã gửi Ephemeral message qua Channel tới User {UserId} tại Channel {ChannelId}", userId, channelId);
                return;
            }

            var messageContent = new MessageContentBuilder().SetText(text).Build();
            var innerMessage = new SendChannelMessageParams(
                clanId: 0,
                channelId: channelId,
                content: messageContent.ToJson(),
                topicId: null,
                isPublic: false
            );

            var ephParams = new SendEphemeralMessageParams(
                receiverIds: new[] { userId },
                message: innerMessage
            );

            await _client.SendEphemeralMessageToBotAsync(ephParams);
            _logger.LogInformation("🤫 [MezonBot] Đã gửi Ephemeral message tới User {UserId} tại Channel {ChannelId}", userId, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi Ephemeral message tới User {UserId}", userId);
        }
    }
}
