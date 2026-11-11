using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using Mezon.Net.Sdk;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Mezon.Net.Core;
using Mezon.Net.Sdk.Builders;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.Modules.MezonBot.Services;
using SdkClient = Mezon.Net.Sdk.MezonClient;

namespace Lingual.Modules.MezonBot;

public class MezonBotWorker : BackgroundService, IMezonBotNotifier
{
    private readonly ILogger<MezonBotWorker> _logger;
    private readonly MezonBotOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReactionBookmarkRateLimiter _reactionRateLimiter;
    private readonly ConcurrentDictionary<long, string> _recentMessageContentCache = new();
    private readonly ConcurrentDictionary<Guid, (long channelId, long mezonUserId)> _userChannelMap = new();
    private SdkClient? _client;

    public class UserRoleplaySession
    {
        public Guid ConversationId { get; set; }
        public string ScenarioCode { get; set; } = string.Empty;
        public string? ScenarioTitle { get; set; }
        public string? CharacterRole { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public UserRoleplaySession() { }

        public UserRoleplaySession(Guid conversationId, string scenarioCode, string? scenarioTitle = null, string? characterRole = null)
        {
            ConversationId = conversationId;
            ScenarioCode = scenarioCode;
            ScenarioTitle = scenarioTitle;
            CharacterRole = characterRole;
            LastActivity = DateTime.UtcNow;
        }
    }

    public static readonly ConcurrentDictionary<string, UserRoleplaySession> ActiveRoleplaySessions = new();
    public static readonly ConcurrentDictionary<Guid, CancellationTokenSource> ActiveQuizTimers = new();
    private static readonly ConcurrentDictionary<string, DateTime> ButtonClickCooldowns = new();
    public static readonly ConcurrentDictionary<long, DateTime> ChannelQuizCooldowns = new();
    private static readonly ConcurrentDictionary<long, long> ChannelClanMap = new();

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

    public class ActiveQuizPollSession
    {
        public string PollKey { get; set; } = string.Empty;
        public long ChannelId { get; set; }
        public long MessageId { get; set; }
        public long PollId { get; set; }
        public int CorrectIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public bool IsSettled { get; set; }
        public object LockObj { get; } = new();

        public ActiveQuizPollSession(string pollKey, long channelId, long messageId, int correctIndex, long pollId = 0)
        {
            PollKey = pollKey;
            ChannelId = channelId;
            MessageId = messageId;
            CorrectIndex = correctIndex;
            PollId = pollId > 0 ? pollId : messageId;
        }
    }

    public record PollSettlementResult(
        string PollKey,
        long ChannelId,
        bool AlreadySettled,
        int TotalVoters,
        int CorrectVotersCount,
        int WrongVotersCount,
        int TotalXpAwarded,
        List<string> AwardedUserIds
    );

    public static readonly ConcurrentDictionary<string, ActiveQuizPollSession> ActiveQuizPollSessions = new();
    public static readonly ConcurrentDictionary<string, CancellationTokenSource> ActivePollTimers = new();

    public static string GetRoleplaySessionKey(long channelId, string senderId) => $"{channelId}_{senderId}";

    public static readonly IReadOnlyList<MessageSelectOption> VocabDeckOptions = new List<MessageSelectOption>
    {
        new MessageSelectOption("📚 Oxford 3000 Core Words", "oxford_3000", "3000 từ vựng cốt lõi tiếng Anh thông dụng nhất"),
        new MessageSelectOption("💼 Business English Essentials", "business_english", "Từ vựng đàm phán, email và thuyết trình kinh doanh"),
        new MessageSelectOption("🎓 Academic Word List (AWL)", "academic_wordlist", "Từ vựng học thuật dành cho IELTS, TOEFL và nghiên cứu"),
        new MessageSelectOption("🗣️ Daily Conversation 500", "daily_conversation", "500 từ vựng hội thoại đời sống giao tiếp hàng ngày")
    };

    public static readonly IReadOnlyList<MessageSelectOption> RoleplayScenarioOptions = new List<MessageSelectOption>
    {
        new MessageSelectOption("☕ Quán Cà phê (A1-A2)", "cafe_order", "Luyện gọi cà phê/trà, chọn size, loại sữa và thanh toán với Barista"),
        new MessageSelectOption("✈️ Sân bay Quốc tế (A2-B1)", "airport_checkin", "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành"),
        new MessageSelectOption("💼 Phỏng vấn xin việc (B1-B2)", "job_interview", "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh"),
        new MessageSelectOption("🚀 Thuyết trình Dự án (C1)", "startup_pitch", "Luyện pitching ý tưởng khởi nghiệp trước hội đồng đầu tư")
    };

    public record ClanQuizPollData(string Question, List<string> Options, int CorrectIndex);

    public static readonly Dictionary<string, ClanQuizPollData> RegisteredClanQuizPolls = new()
    {
        ["poll_vocab_meticulous"] = new ClanQuizPollData(
            "📚 [ĐỐ VUI CLAN] Từ đồng nghĩa của 'Meticulous' là gì?",
            new List<string>
            {
                "A. Careless & Inattentive",
                "B. Thorough & Attentive to detail",
                "C. Aggressive & Impatient",
                "D. Fragile & Delicate"
            },
            1
        ),
        ["poll_vocab_resilient"] = new ClanQuizPollData(
            "📚 [ĐỐ VUI CLAN] 'Resilient' có nghĩa là gì trong ngữ cảnh học tập?",
            new List<string>
            {
                "A. Dễ bỏ cuộc khi gặp khó khăn",
                "B. Kiên cường, phục hồi nhanh sau thất bại",
                "C. Cứng nhắc, không chịu thay đổi",
                "D. Nhút nhát khi giao tiếp"
            },
            1
        )
    };

    public static MessageContent BuildPollQuizMessage(string pollKey = "poll_vocab_meticulous")
    {
        if (!RegisteredClanQuizPolls.TryGetValue(pollKey, out var quizData))
        {
            quizData = RegisteredClanQuizPolls["poll_vocab_meticulous"];
        }

        var pollBuilder = new MessagePollBuilder()
            .SetQuestion(quizData.Question)
            .SetAllowMultipleAnswers(false)
            .SetExpireTime(DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds());

        for (int i = 0; i < quizData.Options.Count; i++)
        {
            pollBuilder.AddAnswer(i, quizData.Options[i]);
        }

        return new MessageContentBuilder()
            .SetText($"📊 Cuộc thi đố vui từ vựng Clan hàng ngày! Chọn đáp án đúng để nhận +10 XP (Id: {pollKey}):")
            .SetPoll(pollBuilder)
            .Build();
    }

    public static MessageContent BuildPollVoteMessage(string? customQuestion = null, List<string>? customOptions = null)
    {
        var question = string.IsNullOrWhiteSpace(customQuestion)
            ? "🗳️ [BẦU CHỌN CLAN] Bạn muốn Clan mở chủ đề luyện nói nào tuần tới?"
            : customQuestion;

        var options = customOptions is { Count: >= 2 }
            ? customOptions
            : new List<string>
            {
                "A. Giao tiếp phỏng vấn tiếng Anh (Tech Interview)",
                "B. Tiếng Anh đàm phán thương mại (Business Negotiation)",
                "C. Thuyết trình dự án & Gọi vốn (Pitching)",
                "D. Du lịch & Văn hóa quốc tế (Travel & Culture)"
            };

        var pollBuilder = new MessagePollBuilder()
            .SetQuestion(question)
            .SetAllowMultipleAnswers(true)
            .SetExpireTime(DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds());

        for (int i = 0; i < options.Count; i++)
        {
            pollBuilder.AddAnswer(i, options[i]);
        }

        return new MessageContentBuilder()
            .SetText("🗳️ Cuộc bình chọn ý kiến thành viên Clan:")
            .SetPoll(pollBuilder)
            .Build();
    }

    /// <summary>
    /// Phân giải định tuyến loại lệnh từ tin nhắn Mezon để kiểm soát dispatch chính xác và hỗ trợ unit test.
    /// </summary>
    public static string ResolveCommandType(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        var trimmed = text.Trim();

        if (trimmed.Equals("/ping", StringComparison.OrdinalIgnoreCase))
            return "ping";
        if (trimmed.Equals("/help", StringComparison.OrdinalIgnoreCase))
            return "help";
        if (trimmed.Equals("/learn", StringComparison.OrdinalIgnoreCase))
            return "learn";
        if (trimmed.Equals("/daily", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("/checkin", StringComparison.OrdinalIgnoreCase))
            return "daily";
        if (trimmed.Equals("/streak", StringComparison.OrdinalIgnoreCase))
            return "streak";
        if (trimmed.Equals("/quiz", StringComparison.OrdinalIgnoreCase))
            return "quiz";
        if (trimmed.StartsWith("/roleplay", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/scenario", StringComparison.OrdinalIgnoreCase))
            return "roleplay";
        if (trimmed.StartsWith("/vocab", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/deck", StringComparison.OrdinalIgnoreCase))
            return "vocab";
        if (trimmed.StartsWith("/poll", StringComparison.OrdinalIgnoreCase))
            return "poll";
        if (trimmed.Equals("/top", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("/leaderboard", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("/bxh", StringComparison.OrdinalIgnoreCase))
            return "leaderboard";
        if (trimmed.StartsWith("/ask", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/check", StringComparison.OrdinalIgnoreCase))
            return "ask";

        return "unknown";
    }

    private readonly DmConversationRateLimiter _dmRateLimiter;
    public ReactionBookmarkRateLimiter ReactionRateLimiter => _reactionRateLimiter;
    public DmConversationRateLimiter DmRateLimiter => _dmRateLimiter;
    public ConcurrentDictionary<long, string> RecentMessageContentCache => _recentMessageContentCache;
    public ConcurrentDictionary<Guid, (long channelId, long mezonUserId)> UserChannelMap => _userChannelMap;
    public readonly ConcurrentBag<(long ChannelId, long UserId, string Text)> SentEphemeralMessages = new();
    public readonly ConcurrentBag<(long ChannelId, long UserId, MessageContent Content)> SentEphemeralContents = new();
    public readonly ConcurrentBag<(long ClanId, long ChannelId, long MessageId, string Emoji, bool Success)> ReactionHistory = new();

    /// <summary>
    /// Provider hàm delay cho các tác vụ retry (cho phép unit test override để chạy tức thì không bị sleep).
    /// </summary>
    public Func<TimeSpan, Task> RetryDelayProvider { get; set; } = Task.Delay;

    /// <summary>
    /// Delegate thực hiện gọi API thả reaction lên message (cho phép unit test bypass network Gateway thực tế).
    /// </summary>
    public Func<Mezon.Net.Sdk.Entities.Message, long, string, long, Task> MessageReactionSender { get; set; } =
        (msg, emojiId, emoji, botId) => msg.AddReactionAsync(emojiId, emoji, botId);

    /// <summary>
    /// Cho phép unit test thiết lập SdkClient giả lập để kiểm thử logic xử lý nội bộ.
    /// </summary>
    public void SetClientForTesting(SdkClient client)
    {
        _client = client;
    }

    public MezonBotWorker(
        ILogger<MezonBotWorker> logger,
        IOptions<MezonBotOptions> options,
        IServiceScopeFactory scopeFactory,
        ReactionBookmarkRateLimiter? reactionRateLimiter = null,
        DmConversationRateLimiter? dmRateLimiter = null)
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _reactionRateLimiter = reactionRateLimiter ?? new ReactionBookmarkRateLimiter(10);
        _dmRateLimiter = dmRateLimiter ?? new DmConversationRateLimiter(20);
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
                _logger.LogInformation("🚀 [MezonBot] Đang khởi tạo kết nối Mezon.Net.Sdk 1.11.0 cho BotId: {BotId}...", botId);

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
                _client.DropdownBoxSelected += OnDropdownBoxSelectedAsync;
                _client.UserChannelAdded += OnUserChannelAddedAsync;
                _client.MessageReactionReceived += OnMessageReactionReceivedAsync;

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
                        _client.DropdownBoxSelected -= OnDropdownBoxSelectedAsync;
                        _client.UserChannelAdded -= OnUserChannelAddedAsync;
                        _client.MessageReactionReceived -= OnMessageReactionReceivedAsync;
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
            var joinedCount = 0;

            // Quét danh sách kênh DM/Private qua API công khai _client.ListChannelDescsAsync (clanId: 0, channelType: Dm)
            try
            {
                var requestParams = new ListChannelDescsParams(
                    limit: null,
                    state: null,
                    cursor: null,
                    clanId: 0,
                    channelType: (int)ChannelType.Dm,
                    isMobile: null,
                    page: null
                );
                var userChannels = await _client.ListChannelDescsAsync(requestParams, null);
                if (userChannels.Channeldesc.Count > 0)
                {
                    foreach (var ch in userChannels.Channeldesc)
                    {
                        if (ch.ClanId == 0 || ch.ChannelPrivate > 0)
                        {
                            ChannelClanMap[ch.ChannelId] = ch.ClanId;
                            var channel = await _client.GetChannelAsync(ch.ChannelId);
                            if (channel != null)
                            {
                                await channel.JoinAsync();
                                joinedCount++;
                                _logger.LogInformation("🔗 [MezonBot] Đã kết nối lắng nghe kênh DM/Private {ChannelId} thành công qua Channel.JoinAsync!", ch.ChannelId);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ [MezonBot] Không tìm thấy Channel entity cho kênh DM {ChannelId} để join qua Channel.JoinAsync", ch.ChannelId);
                            }
                        }
                    }
                }
            }
            catch (Exception exUser)
            {
                _logger.LogDebug(exUser, "ListChannelDescsAsync quét kênh DM hoàn tất hoặc gặp lỗi kết nối ban đầu.");
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
                ChannelClanMap[ch.ChannelId] = ch.ClanId;
                if (_client != null)
                {
                    var channel = await _client.GetChannelAsync(ch.ChannelId);
                    if (channel != null)
                    {
                        await channel.JoinAsync();
                        _logger.LogInformation("🔗 [MezonBot] Đã tự động kết nối lắng nghe kênh DM mới {ChannelId} qua Channel.JoinAsync!", ch.ChannelId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [MezonBot] Không tìm thấy Channel entity cho kênh DM mới {ChannelId} để tự động kết nối qua Channel.JoinAsync", ch.ChannelId);
                    }
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
            ChannelClanMap[data.ChannelId] = data.ClanId;
            _logger.LogInformation("📩 [MezonBot RAW] Channel: {ChannelId}, Clan: {ClanId}, Public: {IsPublic}, Sender: {SenderId}",
                data.ChannelId, data.ClanId, data.IsPublic, data.SenderId);

            var rawContent = data.Content;
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return;
            }

            if (data.MessageId > 0)
            {
                _recentMessageContentCache[data.MessageId] = rawContent;
                if (_recentMessageContentCache.Count > 2000)
                {
                    var oldest = _recentMessageContentCache.Keys.Take(500).ToList();
                    foreach (var k in oldest) _recentMessageContentCache.TryRemove(k, out _);
                }
            }

            if (data.SenderId > 0 && data.ChannelId > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
                        var uid = await userResolver.ResolveUserIdAsync(data.SenderId.ToString(), data.Username);
                        if (uid != Guid.Empty)
                        {
                            _userChannelMap[uid] = (data.ChannelId, data.SenderId);
                        }
                    }
                    catch
                    {
                        // Ignore background user channel resolution failure
                    }
                });
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
                await ReplyToChannelAsync(data.ChannelId, "🏓 Pong! Bot LINGUAL hoạt động bình thường trên nền tảng Mezon Campus Studio 2026. (SDK: Mezon.Net.Sdk 1.11.0)", data);
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
            else if (text.StartsWith("/vocab", StringComparison.OrdinalIgnoreCase) ||
                     text.StartsWith("/deck", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                await HandleVocabDeckCommandAsync(data.ChannelId, senderId, data);
            }
            else if (text.StartsWith("/poll", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                var subCommand = text.Length > 5 ? text[5..].Trim() : string.Empty;
                await HandlePollCommandAsync(data.ChannelId, senderId, subCommand, data);
            }
            else if (text.Equals("/streak", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                var displayName = string.IsNullOrWhiteSpace(data.Username)
                    ? $"Member_{senderId[..Math.Min(6, senderId.Length)]}"
                    : data.Username;
                long.TryParse(senderId, out var uid);
                await HandleStreakCommandAsync(data.ChannelId, senderId, displayName, data, ephemeralUserId: uid > 0 ? uid : null);
            }
            else if (text.Equals("/top", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/leaderboard", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/bxh", StringComparison.OrdinalIgnoreCase))
            {
                var senderId = data.SenderId.ToString();
                long.TryParse(senderId, out var uid);
                await HandleLeaderboardCommandAsync(data.ChannelId, senderId, data, ephemeralUserId: uid > 0 ? uid : null);
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
            else if (isDm && !text.StartsWith("/"))
            {
                var senderId = data.SenderId.ToString();
                var displayName = string.IsNullOrWhiteSpace(data.Username)
                    ? $"Learner_{senderId[..Math.Min(6, senderId.Length)]}"
                    : data.Username;
                await HandleDmConversationAsync(data.ChannelId, senderId, displayName, text, data);
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
            .AddField("🔥 Duy trì Streak & Thi đua", "• /daily: Điểm danh nhận +20 XP và duy trì ngọn lửa Streak mỗi ngày\n• /streak: Tra cứu chuỗi ngày học và kỷ lục của bạn\n• /top: Bảng xếp hạng tuần Top 10 cao thủ LINGUAL", false)
            .AddField("📚 Học tập & Luyện ngữ pháp", "• /ask <câu>: Trợ lý LingLing phân tích & sửa lỗi ngữ pháp\n• /roleplay: Đóng vai luyện giao tiếp tiếng Anh qua Dropdown Select Menu\n• /vocab: Chọn bộ từ vựng luyện tập theo chủ đề qua Dropdown Select\n• /learn: Ôn tập từ vựng qua thẻ ghi nhớ 3D thông minh", false)
            .AddField("🏆 Đấu trường Clan & Khảo sát", "• /quiz: Tham gia đố vui từ vựng 30 giây tranh tài cùng Clan\n• /poll: Tham gia cuộc thi đố vui nhận +10 XP (/poll quiz) hoặc bình chọn chủ đề (/poll vote)", false)
            .AddField("🌐 Webview Miniapp Học tập", $"• AI Tutor LingLing: {_options.WebAppUrl}/tutor\n• Luyện tập Quiz: {_options.WebAppUrl}/quiz\n• Bảng xếp hạng: {_options.WebAppUrl}/leaderboard\n• Thẻ ghi nhớ 3D: {_options.WebAppUrl}/learn", false)
            .AddField("⚡ Tiện ích & Trạng thái", "• /ping: Kiểm tra kết nối của Bot\n• /help: Xem danh sách lệnh trợ giúp", false)
            .SetFooter("Powered by LINGUAL • Mezon Campus Studio 2026", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_daily_checkin", "🔥 Điểm danh /daily", 1, false, "", "")
            .AddButton("btn_view_streak", "🔥 Chuỗi của tôi", 2, false, "", "")
            .AddButton("btn_roleplay_menu", "🎭 Luyện Roleplay", 2, false, "", "")
            .AddButton("btn_open_miniapp", "🚀 Mở Mini-App", 1, false, "", "")
            .AddButton("btn_view_leaderboard", "🏆 Bảng xếp hạng", 3, false, "", "")
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
            .AddButton("btn_view_streak", "🔥 Xem Streak", 2, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText("🎯 Cùng LingLing tích lũy từ vựng mỗi ngày:")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content, replyTo);
    }

    public async Task HandleDailyCommandAsync(
        long channelId,
        string senderId,
        string displayName,
        ChannelMessageResponse? replyTo = null,
        long? ephemeralUserId = null)
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

            if (ephemeralUserId.HasValue && ephemeralUserId.Value > 0)
            {
                await SendEphemeralMessageWithContentAsync(channelId, ephemeralUserId.Value, content);
            }
            else
            {
                await ReplyToChannelWithContentAsync(channelId, content, replyTo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /daily cho User {UserId}", senderId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi thực hiện điểm danh. Vui lòng thử lại sau!", replyTo);
        }
    }

    public async Task HandleStreakCommandAsync(
        long channelId,
        string mezonUserId,
        string? displayName,
        ChannelMessageResponse? replyTo = null,
        long? ephemeralUserId = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
            var gamificationService = scope.ServiceProvider.GetService<IGamificationService>();

            if (gamificationService == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ Gamification (Streak/Leaderboard) chưa sẵn sàng trên hệ thống.", replyTo);
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

            embedBuilder.AddField("⚡ Tiến độ XP hôm nay", $"{progressBar} {streak.TodayXp}/{targetXp} XP ({progressPct}%)\n" +
                (streak.IsActiveToday ? "✅ Đã đạt mục tiêu giữ chuỗi ngày hôm nay!" : $"⏳ Cần thêm {streak.XpNeededForStreak} XP để giữ chuỗi."), false);

            embedBuilder.SetFooter("Duy trì Streak mỗi ngày để nhân hệ số XP và leo Rank! • LINGUAL", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_daily_checkin", "🔥 Điểm danh ngay (+20 XP)", 1, false, "", "")
                .AddButton("btn_open_miniapp", "🚀 Bắt đầu học", 2, false, "", "")
                .AddButton("btn_view_leaderboard", "🏆 Bảng xếp hạng", 3, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🔥 Thông tin chuỗi thói quen của bạn:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            if (ephemeralUserId.HasValue && ephemeralUserId.Value > 0)
            {
                await SendEphemeralMessageWithContentAsync(channelId, ephemeralUserId.Value, content);
            }
            else
            {
                await ReplyToChannelWithContentAsync(channelId, content, replyTo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /streak cho User {UserId}", mezonUserId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi kiểm tra chuỗi Streak. Vui lòng thử lại sau!", replyTo);
        }
    }

    public async Task HandleLeaderboardCommandAsync(
        long channelId,
        string mezonUserId,
        ChannelMessageResponse? replyTo = null,
        long? ephemeralUserId = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
            var gamificationService = scope.ServiceProvider.GetService<IGamificationService>();

            if (gamificationService == null)
            {
                await ReplyToChannelAsync(channelId, "⚠️ Dịch vụ Gamification (Leaderboard) chưa sẵn sàng trên hệ thống.", replyTo);
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
                    var isMe = entry.UserId == userId.ToString() ? " 👈 (Bạn)" : "";
                    sb.AppendLine($"{medal} **{entry.Name}** — `{entry.Xp:N0} XP`{isMe}");
                }
                embedBuilder.AddField("Top 10 Cao Thủ", sb.ToString(), false);
            }

            if (leaderboard.MyRank != null)
            {
                embedBuilder.AddField("Thứ hạng của bạn",
                    $"Bạn đang đứng hạng **#{leaderboard.MyRank.Rank}** với **{leaderboard.MyRank.Xp:N0} XP**.",
                    false);
            }

            embedBuilder.SetFooter("Bảng xếp hạng reset lúc 00:00 thứ Hai hàng tuần • LINGUAL", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_view_streak", "🔥 Xem Streak", 1, false, "", "")
                .AddButton("btn_open_miniapp", "🚀 Học bài tích XP", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🏆 Bảng vàng vinh danh LINGUAL tuần này:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            if (ephemeralUserId.HasValue && ephemeralUserId.Value > 0)
            {
                await SendEphemeralMessageWithContentAsync(channelId, ephemeralUserId.Value, content);
            }
            else
            {
                await ReplyToChannelWithContentAsync(channelId, content, replyTo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý lệnh /leaderboard");
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi tải bảng xếp hạng. Vui lòng thử lại sau!", replyTo);
        }
    }

    public virtual async Task HandleQuizCommandAsync(
        long channelId,
        string? senderId,
        bool isPractice = false,
        ChannelMessageResponse? replyTo = null)
    {
        try
        {
            var now = DateTime.UtcNow;

            // 1. Kiểm tra Cooldown 60 giây theo kênh
            if (ChannelQuizCooldowns.TryGetValue(channelId, out var lastQuizTime))
            {
                var elapsed = (now - lastQuizTime).TotalSeconds;
                if (elapsed < 60)
                {
                    var remainingSec = (int)Math.Max(1, Math.Ceiling(60 - elapsed));
                    var uid = long.TryParse(senderId, out var pUid) && pUid > 0 ? pUid : (replyTo?.SenderId ?? 0);
                    var cooldownMsg = $"⏳ Lệnh /quiz đang trong thời gian chờ (cooldown). Vui lòng thử lại sau {remainingSec} giây!";
                    if (uid > 0)
                    {
                        await SendEphemeralMessageAsync(channelId, uid, cooldownMsg);
                    }
                    else
                    {
                        await ReplyToChannelAsync(channelId, cooldownMsg, replyTo);
                    }
                    return;
                }
            }

            using var scope = _scopeFactory.CreateScope();
            var reactionQuizService = scope.ServiceProvider.GetRequiredService<IReactionQuizService>();

            // 2. Kiểm tra kênh đã có câu hỏi Quiz nào đang mở (open) chưa
            var activeSession = await reactionQuizService.GetActiveSessionAsync(channelId);
            if (activeSession.HasActive)
            {
                var remainingMin = activeSession.ClosesAt.HasValue
                    ? (int)Math.Max(1, Math.Ceiling((activeSession.ClosesAt.Value - now).TotalMinutes))
                    : 10;
                var uid = long.TryParse(senderId, out var pUid) && pUid > 0 ? pUid : (replyTo?.SenderId ?? 0);
                var activeMsg = $"⚠️ Kênh này đang có một câu hỏi Quiz đang diễn ra (còn lại khoảng {remainingMin} phút)! Vui lòng hoàn thành câu hỏi hiện tại trước khi tạo mới.";
                if (uid > 0)
                {
                    await SendEphemeralMessageAsync(channelId, uid, activeMsg);
                }
                else
                {
                    await ReplyToChannelAsync(channelId, activeMsg, replyTo);
                }
                return;
            }

            var quizDto = await reactionQuizService.CreateQuizAsync(channelId, replyTo?.ClanId ?? 0, senderId);

            // Xây dựng nội dung Embed: TUYỆT ĐỐI KHÔNG chứa đáp án đúng hay is_correct!
            var optionsText = string.Join("\n", quizDto.Options.Select(o =>
            {
                var flagEmoji = o.Key.ToUpperInvariant() switch
                {
                    "A" => "🇦",
                    "B" => "🇧",
                    "C" => "🇨",
                    "D" => "🇩",
                    _ => "🔹"
                };
                return $"{flagEmoji} **{o.Key}**. {o.Text}";
            }));

            var embed = new MessageEmbedBuilder()
                .SetColor("#3B82F6")
                .SetTitle("🎯 ĐỐ VUI TIẾNG ANH (REACTION QUIZ) — LINGUAL")
                .SetDescription("Hãy chọn đáp án chính xác bằng cách thả reaction cờ tương ứng vào tin nhắn này!\n⏱️ **Thời hạn:** 10 phút | ⚡ **Thưởng:** **+10 XP** cho đáp án chính xác")
                .AddField("📝 Câu hỏi", quizDto.QuestionPrompt, false)
                .AddField("🎯 Các lựa chọn", optionsText, false)
                .AddField("📊 Thông tin", $"Trình độ: {quizDto.Level ?? "A1-A2"} • Độ khó: {quizDto.Difficulty ?? 1}/5", false)
                .SetFooter("Thả reaction 🇦 / 🇧 / 🇨 / 🇩 để trả lời • Mỗi bạn chỉ được tính 1 lần", "")
                .Build();

            var content = new MessageContentBuilder()
                .SetText("🔥 Đố vui Reaction Quiz vừa xuất hiện! Hãy thả reaction cờ 🇦, 🇧, 🇨 hoặc 🇩 để trả lời:")
                .AddEmbed(embed)
                .Build();

            var sentMessageId = await ReplyToChannelWithContentAsync(channelId, content, replyTo);
            if (sentMessageId > 0)
            {
                ChannelQuizCooldowns[channelId] = DateTime.UtcNow;
                await reactionQuizService.LinkMessageAsync(quizDto.SessionId, sentMessageId);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var clanId = replyTo?.ClanId ?? 0;
                        await AddReactionToMessageAsync(clanId, channelId, sentMessageId, "🇦");
                        await AddReactionToMessageAsync(clanId, channelId, sentMessageId, "🇧");
                        await AddReactionToMessageAsync(clanId, channelId, sentMessageId, "🇨");
                        await AddReactionToMessageAsync(clanId, channelId, sentMessageId, "🇩");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ [MezonBot] Không thể thêm reaction mẫu vào Reaction Quiz {MessageId}", sentMessageId);
                    }
                });

                // Đăng ký tác vụ trễ 10 phút đánh dấu hết hạn phiên quiz
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(10));
                        await ConcludeReactionQuizExpiredAsync(channelId, sentMessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi tác vụ trễ đóng Quiz {MessageId}", sentMessageId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi tạo câu đố Reaction Quiz tại Channel {ChannelId}", channelId);
            await ReplyToChannelAsync(channelId, "❌ Đã có lỗi xảy ra khi tạo câu đố. Vui lòng thử lại sau!", replyTo);
        }
    }

    public virtual async Task ConcludeReactionQuizExpiredAsync(long channelId, long messageId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reactionQuizService = scope.ServiceProvider.GetRequiredService<IReactionQuizService>();

            var result = await reactionQuizService.ConcludeExpiredQuizAsync(messageId);
            if (result == null) return;

            var correctText = !string.IsNullOrEmpty(result.CorrectKey)
                ? $"\n🎯 Đáp án chính xác: Phương án **{result.CorrectKey.ToUpperInvariant()}**"
                : string.Empty;

            var expiredEmbed = new MessageEmbedBuilder()
                .SetColor("#6B7280") // Gray Muted
                .SetTitle("🎯 ĐỐ VUI TIẾNG ANH (REACTION QUIZ) — [ĐÃ HẾT HẠN]")
                .SetDescription($"⏰ Phiên đố vui này đã kết thúc nhận đáp án! (Thời gian tối đa 10 phút){correctText}")
                .AddField("📝 Câu hỏi", result.QuestionPrompt, false)
                .SetFooter("Phiên đã kết thúc • Gõ /quiz để mở câu đố mới sau cooldown • LINGUAL", "")
                .Build();

            var content = new MessageContentBuilder()
                .SetText("⏰ Phiên Reaction Quiz này đã kết thúc nhận đáp án:")
                .AddEmbed(expiredEmbed)
                .Build();

            if (_client != null)
            {
                var channel = await _client.GetChannelAsync(channelId);
                if (channel != null)
                {
                    await channel.UpdateMessageAsync(messageId, content);
                    _logger.LogInformation("🏷️ [MezonBot] Đã sửa embed thành 'Đã hết hạn' cho Quiz {MessageId}", messageId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Không thể sửa embed hết hạn cho Quiz {MessageId} (best effort)", messageId);
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
            var effectiveUserId = data.UserId > 0 ? data.UserId : data.SenderId;
            var mezonUserId = effectiveUserId > 0 ? effectiveUserId.ToString() : data.UserId.ToString();

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
                var uid = effectiveUserId > 0 ? effectiveUserId : (long.TryParse(mezonUserId, out var pUid) ? pUid : 0);
                await HandleDailyCommandAsync(channelId, mezonUserId, username, ephemeralUserId: uid > 0 ? uid : null);
            }
            else if (buttonId == "btn_quiz_start_prompt")
            {
                var isDm = ChannelClanMap.TryGetValue(channelId, out var cid) ? (cid <= 0) : false;
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
            else if (buttonId == "btn_view_streak")
            {
                var displayName = $"Learner_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";
                var uid = effectiveUserId > 0 ? effectiveUserId : (long.TryParse(mezonUserId, out var pUid) ? pUid : 0);
                await HandleStreakCommandAsync(channelId, mezonUserId, displayName, ephemeralUserId: uid > 0 ? uid : null);
            }
            else if (buttonId == "btn_view_leaderboard" || buttonId == "btn_leaderboard")
            {
                var uid = effectiveUserId > 0 ? effectiveUserId : (long.TryParse(mezonUserId, out var pUid) ? pUid : 0);
                await HandleLeaderboardCommandAsync(channelId, mezonUserId, ephemeralUserId: uid > 0 ? uid : null);
            }
            else if (buttonId == "btn_srs_review_now")
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
                    var flashcardService = scope.ServiceProvider.GetRequiredService<IFlashcardService>();
                    var userId = await userResolver.ResolveUserIdAsync(mezonUserId);
                    var dueCards = await flashcardService.GetDueCardsAsync(userId, 5);

                    if (dueCards.Count == 0)
                    {
                        await SendEphemeralMessageAsync(channelId, uid, "🎉 LingLing: Bạn đã ôn tập hết các từ vựng đến hạn hôm nay rồi! Thật xuất sắc, hãy tiếp tục duy trì phong độ!");
                    }
                    else
                    {
                        var embedBuilder = new MessageEmbedBuilder()
                            .SetColor("#6366F1") // Indigo
                            .SetTitle("📚 ÔN TẬP TỪ VỰNG SRS — LINGUAL")
                            .SetDescription($"Chào bạn, đây là **{dueCards.Count} từ vựng** đến hạn cần ôn tập hôm nay để củng cố trí nhớ dài hạn (Spaced Repetition):");

                        int idx = 1;
                        foreach (var c in dueCards)
                        {
                            embedBuilder.AddField($"{idx++}. 🔹 {c.Term}", $"• Ý nghĩa: **{c.Meaning}**\n• Chu kỳ: {c.IntervalDays} ngày | Lần ôn: {c.Repetitions}", false);
                        }

                        embedBuilder.SetFooter("Hoàn thành ôn tập mỗi ngày để giữ vững Streak và củng cố trí nhớ! • LINGUAL", "");

                        var actionButtons = new ButtonBuilder()
                            .AddButton("btn_open_miniapp", "🚀 Mở Webview luyện Quiz", 1, false, "", "")
                            .BuildComponents();

                        var content = new MessageContentBuilder()
                            .SetText("📖 Danh sách từ vựng SRS cần ôn tập:")
                            .AddEmbed(embedBuilder.Build())
                            .AddActionRow(actionButtons)
                            .Build();

                        await SendEphemeralMessageWithContentAsync(channelId, uid, content);
                    }
                }
            }
            else if (buttonId == "btn_use_freeze")
            {
                if (long.TryParse(mezonUserId, out var uid))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var streakService = scope.ServiceProvider.GetRequiredService<IUserStreakService>();
                    var username = $"Member_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}";
                    var result = await streakService.UseStreakFreezeAsync(mezonUserId, username);

                    if (result.IsSuccess)
                    {
                        var embedBuilder = new MessageEmbedBuilder()
                            .SetColor("#10B981") // Emerald Green
                            .SetTitle("🛡️ KÍCH HOẠT KHIÊN ĐÓNG BĂNG THÀNH CÔNG!")
                            .SetDescription($"Chúc mừng **{username}**! Chuỗi học tập **{result.CurrentDays} ngày** của bạn đã được đóng băng bảo vệ an toàn cho ngày hôm nay.\n\n🛡️ **Số khiên Streak Freeze còn lại:** {result.RemainingFreezes} khiên.")
                            .SetFooter("LINGUAL Streak Protection System • Mezon Campus Studio 2026", "");

                        var content = new MessageContentBuilder()
                            .SetText("✨ Trạng thái bảo vệ Streak của bạn:")
                            .AddEmbed(embedBuilder.Build())
                            .Build();

                        await SendEphemeralMessageWithContentAsync(channelId, uid, content);
                    }
                    else
                    {
                        await SendEphemeralMessageAsync(channelId, uid, $"⚠️ LingLing: {result.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện click button");
        }
    }

    private async Task OnDropdownBoxSelectedAsync(DropdownBoxSelectedEventData eventData)
    {
        try
        {
            var response = (DropdownBoxSelectedResponse)eventData;
            var channelId = response.ChannelId;
            var mezonUserId = (response.UserId != 0 ? response.UserId : response.SenderId).ToString();
            var selectboxId = response.SelectboxId ?? string.Empty;
            var selectedValue = response.Values.Count > 0 ? response.Values[0] : string.Empty;

            _logger.LogInformation("📥 [MezonBot] DropdownBoxSelected: SelectboxId={SelectboxId}, Value={Value}, User={User}",
                selectboxId, selectedValue, mezonUserId);

            if (string.IsNullOrWhiteSpace(selectedValue)) return;

            if (selectboxId == "select_roleplay_scenario")
            {
                await HandleRoleplayScenarioSelectedAsync(channelId, mezonUserId, selectedValue);
            }
            else if (selectboxId == "select_vocab_deck")
            {
                await HandleVocabDeckSelectedAsync(channelId, mezonUserId, selectedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý sự kiện DropdownBoxSelected");
        }
    }

    public static bool IsBookmarkEmoji(string? emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji)) return false;
        var e = emoji.Trim().ToLowerInvariant();
        return e == "🔖" || e == "⭐" || e == "📌" || e == "🏷️"
            || e.Contains("bookmark") || e.Contains("star") || e.Contains("pin");
    }

    public async Task OnMessageReactionReceivedAsync(MessageReactionEventData eventData)
    {
        try
        {
            MessageReactionResponse reaction = (MessageReactionResponse)eventData;
            if (!reaction.Action)
            {
                return;
            }

            // Bỏ qua reaction của chính Bot (tránh bot tự trả lời quiz hoặc tự bookmark)
            var botId = _options.GetEffectiveBotId();
            if (botId > 0 && reaction.SenderId == botId)
            {
                _logger.LogInformation("ℹ️ [MezonBot] Bỏ qua reaction từ chính bot (SenderId={SenderId})", reaction.SenderId);
                return;
            }

            // 1. ADV-03: Reaction Bookmark (nếu là emoji bookmark)
            if (IsBookmarkEmoji(reaction.Emoji))
            {
                await HandleMessageReactionBookmarkAsync(
                    reaction.ClanId,
                    reaction.ChannelId,
                    reaction.MessageId,
                    reaction.SenderId,
                    reaction.SenderName,
                    reaction.Emoji,
                    reaction.Action);
                return;
            }

            // 2. ADV-07: Reaction Quiz (nếu là emoji cờ A/B/C/D)
            var quizOption = ReactionQuizService.ParseQuizEmoji(reaction.Emoji);
            if (quizOption != null)
            {
                await HandleMessageReactionQuizAsync(
                    reaction.ClanId,
                    reaction.ChannelId,
                    reaction.MessageId,
                    reaction.SenderId,
                    reaction.SenderName,
                    reaction.Emoji);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi xử lý MessageReactionReceived.");
        }
    }

    public async Task<bool> HandleMessageReactionBookmarkAsync(
        long clanId,
        long channelId,
        long messageId,
        long senderId,
        string senderName,
        string emoji,
        bool action)
    {
        var botId = _options.GetEffectiveBotId();
        if (botId > 0 && senderId == botId)
        {
            _logger.LogInformation("ℹ️ [MezonBot] Bỏ qua reaction bookmark từ chính Bot (SenderId={SenderId})", senderId);
            return false;
        }
        if (!action)
        {
            return false;
        }

        if (!IsBookmarkEmoji(emoji))
        {
            return false;
        }

        if (senderId <= 0 || channelId <= 0 || messageId <= 0)
        {
            return false;
        }

        // Rate limit: Tối đa 10 bookmark / phút / user
        if (!_reactionRateLimiter.TryAcquire(senderId.ToString()))
        {
            _logger.LogWarning("⚠️ [MezonBot] User {SenderId} bị rate limit reaction bookmark (quá 10 lần/phút).", senderId);
            await SendEphemeralMessageAsync(channelId, senderId, "⚠️ LingLing: Bạn đang thao tác bookmark quá nhanh (giới hạn tối đa 10 lần/phút). Vui lòng thử lại sau ít phút!");
            return false;
        }

        // F1 fix: Kiểm tra nội dung tin nhắn trong cache, tránh tạo thẻ rác khi cache miss
        if (!_recentMessageContentCache.TryGetValue(messageId, out var rawContent) || string.IsNullOrWhiteSpace(rawContent))
        {
            _logger.LogWarning("⚠️ [MezonBot] Reaction bookmark tin ngoài cache (Message {MessageId}). Không tìm thấy nội dung để tạo thẻ.", messageId);
            await SendEphemeralMessageAsync(channelId, senderId,
                "⚠️ LingLing: Không thể đọc nội dung tin nhắn này để lưu thẻ từ vựng (tin nhắn có thể được gửi trước khi bot khởi động hoặc đã hết hạn trong bộ nhớ đệm). Vui lòng thử bookmark với tin nhắn mới hơn!");
            return false;
        }

        string messageText = string.Empty;
        try
        {
            var parsed = MessageContent.Parse(rawContent);
            messageText = parsed.Text?.Trim() ?? rawContent.Trim();
        }
        catch
        {
            messageText = rawContent.Trim();
        }

        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("⚠️ [MezonBot] Tin nhắn {MessageId} trong cache có nội dung rỗng.", messageId);
            await SendEphemeralMessageAsync(channelId, senderId,
                "⚠️ LingLing: Tin nhắn này không chứa nội dung văn bản hợp lệ để tạo thẻ từ vựng. Vui lòng thử với tin nhắn khác!");
            return false;
        }

        var (term, meaning) = VocabularyExtractor.ExtractTermAndMeaning(messageText);
        if (string.IsNullOrWhiteSpace(term))
        {
            _logger.LogWarning("⚠️ [MezonBot] Không thể trích xuất từ vựng từ tin nhắn {MessageId}.", messageId);
            await SendEphemeralMessageAsync(channelId, senderId,
                "⚠️ LingLing: Không thể trích xuất từ vựng từ tin nhắn này. Vui lòng thử lại với tin nhắn có định dạng rõ ràng hơn!");
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
        var flashcardService = scope.ServiceProvider.GetRequiredService<IFlashcardService>();

        var userId = await userResolver.ResolveUserIdAsync(senderId.ToString(), senderName);
        if (userId != Guid.Empty)
        {
            _userChannelMap[userId] = (channelId, senderId);
        }

        var card = await flashcardService.CreateOrBookmarkFlashcardAsync(userId, term, meaning);

        // Gửi Ephemeral xác nhận tới người dùng
        await SendEphemeralMessageAsync(channelId, senderId,
            $"🔖 LingLing: Đã lưu từ vựng **{card.Term}** (*{card.Meaning}*) vào sổ tay Flashcard SRS của bạn!\n(Chu kỳ ôn tập ban đầu: {card.IntervalDays} ngày • Hệ số dễ: {card.EaseFactor})");

        // Bot thêm reaction checkmark ✅ vào tin nhắn gốc
        await AddReactionToMessageAsync(clanId, channelId, messageId, "✅");
        return true;
    }

    public async Task<ReactionQuizAnswerResult?> HandleMessageReactionQuizAsync(
        long clanId,
        long channelId,
        long messageId,
        long senderId,
        string senderName,
        string emoji)
    {
        if (senderId <= 0 || channelId <= 0 || messageId <= 0) return null;

        // Bỏ qua nếu người react chính là Bot (tránh bot tự trả lời quiz của chính mình)
        var botId = _options.GetEffectiveBotId();
        if (botId > 0 && senderId == botId)
        {
            _logger.LogInformation("ℹ️ [MezonBot] Bỏ qua reaction quiz từ chính Bot (SenderId={SenderId})", senderId);
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var reactionQuizService = scope.ServiceProvider.GetRequiredService<IReactionQuizService>();

        var result = await reactionQuizService.ProcessReactionAnswerAsync(
            messageId,
            channelId,
            senderId.ToString(),
            senderName,
            emoji);

        if (result.Status == ReactionQuizAnswerStatus.NotAQuizMessage)
        {
            // Reaction vào tin nhắn không phải quiz -> bỏ qua hoàn toàn
            return result;
        }

        if (result.Status == ReactionQuizAnswerStatus.Expired)
        {
            // Quiz hết hạn sau 10 phút: reaction muộn thì gửi ephemeral 'Quiz nay da het han' và đánh dấu embed
            await SendEphemeralMessageAsync(channelId, senderId, result.Message);
            _ = Task.Run(async () =>
            {
                try
                {
                    await ConcludeReactionQuizExpiredAsync(channelId, messageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi cập nhật embed hết hạn sau reaction muộn {MessageId}", messageId);
                }
            });
            return result;
        }

        if (result.Status == ReactionQuizAnswerStatus.AlreadyAnswered)
        {
            _logger.LogInformation("ℹ️ [MezonBot] User {SenderId} đã trả lời Quiz {MessageId} trước đó, bỏ qua reaction thêm.", senderId, messageId);
            return result;
        }

        if (result.Status == ReactionQuizAnswerStatus.Success || result.Status == ReactionQuizAnswerStatus.WrongAnswer)
        {
            await SendEphemeralMessageAsync(channelId, senderId, result.Message);
            return result;
        }

        return result;
    }

    public async Task<bool> HandleDmConversationAsync(
        long channelId,
        string senderId,
        string displayName,
        string text,
        ChannelMessageResponse? replyTo = null)
    {
        // 1. Rate limit: tối đa 20 tin / phút / user
        if (!_dmRateLimiter.TryAcquire(senderId))
        {
            _logger.LogWarning("⚠️ [MezonBot] User {SenderId} vượt rate limit DM 1-1 (quá 20 tin/phút).", senderId);
            long.TryParse(senderId, out var uid);
            var targetUid = uid > 0 ? uid : channelId;
            await SendEphemeralMessageAsync(channelId, targetUid,
                "⚠️ LingLing: Bạn đang gửi tin nhắn quá nhanh (giới hạn tối đa 20 tin/phút). Hãy dành chút thời gian đọc kỹ phản hồi và thử lại sau ít giây nhé!");
            return false;
        }

        // 2. Tái sử dụng engine Gemini / IAITutorService hiện có
        using var scope = _scopeFactory.CreateScope();
        var aiTutor = scope.ServiceProvider.GetService<IAITutorService>();
        if (aiTutor == null)
        {
            await ReplyToChannelAsync(channelId, "⚠️ Trợ lý AI LingLing tạm thời chưa sẵn sàng. Vui lòng thử lại sau!", replyTo);
            return false;
        }

        var sessionKey = GetRoleplaySessionKey(channelId, senderId);
        ActiveRoleplaySessions.TryGetValue(sessionKey, out var currentSession);

        var scenarioCode = currentSession?.ScenarioCode ?? "coffee_shop";
        var conversationId = currentSession?.ConversationId;

        var turnRequest = new RoleplayTurnRequest(
            ScenarioCode: scenarioCode,
            UserMessage: text,
            ConversationId: conversationId,
            MezonUserId: senderId,
            ChannelId: channelId.ToString()
        );

        var turnResponse = await aiTutor.ProcessRoleplayTurnAsync(turnRequest);
        if (turnResponse != null)
        {
            ActiveRoleplaySessions[sessionKey] = new UserRoleplaySession(
                turnResponse.ConversationId,
                turnResponse.ScenarioCode,
                turnResponse.ScenarioTitle,
                turnResponse.CharacterRole
            );

            var replyBuilder = new System.Text.StringBuilder();
            replyBuilder.AppendLine($"🦉 **LingLing ({turnResponse.CharacterRole})**: {turnResponse.Reply}");

            if (turnResponse.HasFeedback && !string.IsNullOrWhiteSpace(turnResponse.FeedbackCorrection))
            {
                replyBuilder.AppendLine();
                replyBuilder.AppendLine($"💡 **Gợi ý tự nhiên hơn**: *{turnResponse.FeedbackCorrection}*");
                if (!string.IsNullOrWhiteSpace(turnResponse.Explanation))
                {
                    replyBuilder.AppendLine($"📖 *{turnResponse.Explanation}*");
                }
            }

            // Trả lời CHỈ trong DM (kênh channelId của DM), TUYỆT ĐỐI KHÔNG gửi ra channel công khai
            // KHÔNG cộng XP cho luyện DM (tránh farm XP trong kênh riêng)
            await ReplyToChannelAsync(channelId, replyBuilder.ToString().Trim(), replyTo);
            return true;
        }

        await ReplyToChannelAsync(channelId, "🦉 LingLing: Mình đã nhận được tin nhắn của bạn! Bạn có thể chia sẻ thêm bằng tiếng Anh không?", replyTo);
        return true;
    }

    public virtual async Task<bool> AddReactionToMessageAsync(long clanId, long channelId, long messageId, string emoji)
    {
        if (_client == null || channelId <= 0 || messageId <= 0)
        {
            _logger.LogWarning("⚠️ [MezonBot] Không thể thêm reaction {Emoji} vào Message {MessageId} (client null hoặc id không hợp lệ)", emoji, messageId);
            ReactionHistory.Add((clanId, channelId, messageId, emoji, false));
            return false;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel == null)
            {
                _logger.LogWarning("⚠️ [MezonBot] Không thể thêm reaction {Emoji} vào Message {MessageId} (không tìm thấy Channel {ChannelId})", emoji, messageId, channelId);
                ReactionHistory.Add((clanId, channelId, messageId, emoji, false));
                return false;
            }

            Mezon.Net.Sdk.Entities.Message? message = null;
            const int maxRetries = 5;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (channel.Messages.TryGet(messageId, out message) && message != null)
                {
                    break;
                }

                if (attempt < maxRetries)
                {
                    await RetryDelayProvider(TimeSpan.FromMilliseconds(400));
                }
            }

            if (message == null)
            {
                _logger.LogWarning("⚠️ [MezonBot] Không thể thêm reaction {Emoji} vào Message {MessageId} sau {MaxRetries} lần thử (không tìm thấy Message trong Channel cache)", emoji, messageId, maxRetries);
                ReactionHistory.Add((clanId, channelId, messageId, emoji, false));
                return false;
            }

            var botId = _options.GetEffectiveBotId();
            await MessageReactionSender(message, 0, emoji, botId);

            _logger.LogInformation("✅ [MezonBot] Đã thêm reaction {Emoji} vào Message {MessageId} tại Channel {ChannelId}", emoji, messageId, channelId);
            ReactionHistory.Add((clanId, channelId, messageId, emoji, true));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi thêm reaction vào Message {MessageId} qua Message.AddReactionAsync", messageId);
            ReactionHistory.Add((clanId, channelId, messageId, emoji, false));
            return false;
        }
    }

    /// <summary>
    /// Gửi tin nhắn trực tiếp (DM 1-1) tới Mezon User thông qua API công khai User.SendDMAsync (SDK 1.11.0).
    /// </summary>
    public virtual async Task<bool> SendDirectMessageAsync(long mezonUserId, MessageContent content)
    {
        if (_client == null || mezonUserId <= 0) return false;
        try
        {
            var user = await _client.GetUserAsync(mezonUserId);
            if (user == null)
            {
                _logger.LogWarning("⚠️ [MezonBot] Không tìm thấy User {UserId} để gửi DM", mezonUserId);
                return false;
            }
            await user.SendDMAsync(content);
            _logger.LogInformation("📨 [MezonBot] Đã gửi DM tới User {UserId} thành công qua API SDK User.SendDMAsync", mezonUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi gửi DM tới User {UserId} qua User.SendDMAsync", mezonUserId);
            return false;
        }
    }

    /// <summary>
    /// Gửi tin nhắn text trực tiếp (DM 1-1) tới Mezon User thông qua API công khai User.SendDMTextAsync (SDK 1.11.0).
    /// </summary>
    public virtual async Task<bool> SendDirectMessageTextAsync(long mezonUserId, string text)
    {
        if (_client == null || mezonUserId <= 0) return false;
        try
        {
            var user = await _client.GetUserAsync(mezonUserId);
            if (user == null)
            {
                _logger.LogWarning("⚠️ [MezonBot] Không tìm thấy User {UserId} để gửi DM text", mezonUserId);
                return false;
            }
            await user.SendDMTextAsync(text);
            _logger.LogInformation("📨 [MezonBot] Đã gửi DM text tới User {UserId} thành công qua API SDK User.SendDMTextAsync", mezonUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [MezonBot] Lỗi khi gửi DM text tới User {UserId} qua User.SendDMTextAsync", mezonUserId);
            return false;
        }
    }

    public (long channelId, long mezonUserId)? GetUserChannelMapping(Guid userId)
    {
        if (_userChannelMap.TryGetValue(userId, out var mapping))
        {
            return mapping;
        }
        return null;
    }

    public async Task<bool> SendSrsReminderAsync(
        Guid userId,
        long channelId,
        long mezonUserId,
        int dueCardsCount,
        int currentStreakDays,
        int freezeBalance,
        CancellationToken cancellationToken = default)
    {
        if (_client == null || channelId <= 0 || mezonUserId <= 0) return false;

        try
        {
            var embedBuilder = new MessageEmbedBuilder()
                .SetColor("#F59E0B")
                .SetTitle("⏰ NHẮC NHỞ ÔN TẬP SRS & NGUY CƠ ĐỨT CHUỖI STREAK!")
                .SetDescription("Chào bạn, bạn có từ vựng đến hạn cần ôn tập hôm nay theo phương pháp Spaced Repetition và mục tiêu Streak hôm nay chưa hoàn thành!")
                .AddField("📚 Thẻ cần ôn hôm nay", $"**{dueCardsCount}** từ vựng", true)
                .AddField("🔥 Chuỗi hiện tại", $"**{currentStreakDays}** ngày liên tục", true)
                .AddField("🛡️ Khiên đóng băng", $"**{freezeBalance}** khiên khả dụng", true)
                .AddField("⚡ Hành động nhanh", "Nhấn **[Ôn 5 từ ngay]** để hoàn thành bài ôn tập 2 phút hoặc dùng **[Dùng khiên đóng băng]** để giữ streak hôm nay!", false)
                .SetFooter("Đừng để ngọn lửa học tập bị dập tắt! • LINGUAL Spaced Repetition", "");

            var buttons = new ButtonBuilder()
                .AddButton("btn_srs_review_now", "📖 Ôn 5 từ ngay", 1, false, "", "")
                .AddButton("btn_use_freeze", "🛡️ Dùng khiên đóng băng", 2, false, "", "")
                .BuildComponents();

            var content = new MessageContentBuilder()
                .SetText("🔔 Thông báo nhắc nhở học tập cá nhân:")
                .AddEmbed(embedBuilder.Build())
                .AddActionRow(buttons)
                .Build();

            // 1. Thử gửi trực tiếp qua API công khai User.SendDMAsync (SDK 1.11.0)
            if (mezonUserId > 0)
            {
                try
                {
                    var user = await _client.GetUserAsync(mezonUserId, cancellationToken);
                    if (user != null)
                    {
                        await user.SendDMAsync(content);
                        _logger.LogInformation("🔔 [MezonBot] Đã gửi thông báo nhắc SRS qua User.SendDMAsync tới Mezon User {MezonUserId}", mezonUserId);
                        return true;
                    }
                }
                catch (Exception exDm)
                {
                    _logger.LogDebug(exDm, "Gửi qua User.SendDMAsync chưa hoàn tất, chuyển sang gửi qua ephemeral channel");
                }
            }

            // 2. Dự phòng qua Ephemeral Channel nếu có channelId
            await SendEphemeralMessageWithContentAsync(channelId, mezonUserId, content);
            _logger.LogInformation("🔔 [MezonBot] Đã gửi thông báo nhắc SRS tới User {UserId} (Mezon {MezonUserId}) qua Ephemeral", userId, mezonUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi thông báo nhắc SRS tới User {UserId}", userId);
            return false;
        }
    }

    public static MessageContent BuildRoleplayScenarioSelectContent()
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#8B5CF6")
            .SetTitle("🎭 LINGLING ROLEPLAY — ĐÓNG VAI GIAO TIẾP TÌNH HUỐNG")
            .SetDescription("Chọn một tình huống thực tế từ menu thả xuống bên dưới để bắt đầu luyện giao tiếp tiếng Anh phản xạ cùng trợ lý LingLing:")
            .AddField("☕ Quán Cà phê (A1-A2)", "Luyện gọi cà phê/trà, chọn size, loại sữa và thanh toán với Barista tại London.", false)
            .AddField("✈️ Sân bay Quốc tế (A2-B1)", "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành tại Heathrow.", false)
            .AddField("💼 Phỏng vấn xin việc (B1-B2)", "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh và định hướng.", false)
            .AddField("🚀 Thuyết trình Dự án (C1)", "Luyện pitching ý tưởng khởi nghiệp và giải đáp câu hỏi của hội đồng đầu tư.", false)
            .SetFooter("Chọn tình huống từ Dropdown Menu bên dưới • LINGUAL Roleplay", "")
            .Build();

        var selectComponents = new ButtonBuilder()
            .AddSelect(
                id: "select_roleplay_scenario",
                options: RoleplayScenarioOptions,
                selectType: MessageSelectType.Text,
                placeholder: "Chọn tình huống giao tiếp...",
                minOptions: 1,
                maxOptions: 1
            )
            .BuildComponents();

        return new MessageContentBuilder()
            .SetText("🎭 Chọn tình huống đàm thoại thực tế từ menu bên dưới:")
            .AddEmbed(embed)
            .AddActionRow(selectComponents)
            .Build();
    }

    public static MessageContent BuildVocabDeckSelectContent()
    {
        var embed = new MessageEmbedBuilder()
            .SetColor("#10B981")
            .SetTitle("📚 LINGUAL VOCABULARY — CHỌN BỘ TỪ VỰNG LUYỆN TẬP")
            .SetDescription("Khám phá các bộ từ vựng tuyển chọn chuẩn quốc tế theo chủ đề và trình độ:")
            .AddField("📚 Oxford 3000 Core Words (A1-B2)", "3000 từ vựng cốt lõi tiếng Anh thông dụng và quan trọng nhất.", false)
            .AddField("💼 Business English (B2-C1)", "Từ vựng đàm phán thương mại, email chuyên nghiệp và pitching.", false)
            .AddField("🎓 Academic Word List (B2-C2)", "Từ vựng học thuật chuyên sâu phục vụ thi IELTS, TOEFL và nghiên cứu.", false)
            .AddField("🗣️ Daily Conversation 500 (A1-A2)", "500 từ vựng và mẫu câu hội thoại đời sống giao tiếp hàng ngày.", false)
            .SetFooter("Chọn bộ từ từ Dropdown Menu bên dưới • LINGUAL Vocabulary", "")
            .Build();

        var selectComponents = new ButtonBuilder()
            .AddSelect(
                id: "select_vocab_deck",
                options: VocabDeckOptions,
                selectType: MessageSelectType.Text,
                placeholder: "Chọn bộ từ vựng luyện tập...",
                minOptions: 1,
                maxOptions: 1
            )
            .BuildComponents();

        return new MessageContentBuilder()
            .SetText("📚 Chọn bộ từ vựng bạn muốn luyện tập hôm nay:")
            .AddEmbed(embed)
            .AddActionRow(selectComponents)
            .Build();
    }

    private async Task HandleVocabDeckCommandAsync(long channelId, string senderId, ChannelMessageResponse? replyTo = null)
    {
        var content = BuildVocabDeckSelectContent();
        await ReplyToChannelWithContentAsync(channelId, content, replyTo);
    }

    private async Task HandleVocabDeckSelectedAsync(long channelId, string senderId, string deckCode)
    {
        var matched = VocabDeckOptions.FirstOrDefault(d => d.Value.Equals(deckCode, StringComparison.OrdinalIgnoreCase));
        var deckName = !string.IsNullOrEmpty(matched.Label) ? matched.Label : deckCode;
        var deckDesc = !string.IsNullOrEmpty(matched.Description) ? matched.Description : "Bộ từ vựng tiếng Anh theo chủ đề";

        var embed = new MessageEmbedBuilder()
            .SetColor("#10B981")
            .SetTitle($"📚 ĐÃ CHỌN BỘ TỪ: {deckName}")
            .SetDescription($"Bạn đã chọn bộ từ vựng thành công! Hãy bắt đầu luyện tập ngay hôm nay:\n\n📖 **Mô tả:** {deckDesc}")
            .AddField("🎯 Chế độ luyện tập", "• Gõ `/learn` để học thẻ ghi nhớ Flashcard 3D thông minh\n• Gõ `/quiz` để thử sức đố vui từ vựng nhanh 30 giây\n• Hoặc mở Webview Mini-App để xem toàn bộ danh sách từ", false)
            .SetFooter("LINGUAL Vocabulary Deck • Mezon Campus Studio", "")
            .Build();

        var buttons = new ButtonBuilder()
            .AddButton("btn_learn_now", "📖 Ôn từ vựng /learn", 1, false, "", "")
            .AddButton("btn_quiz_now", "🏆 Làm Quiz /quiz", 2, false, "", "")
            .BuildComponents();

        var content = new MessageContentBuilder()
            .SetText($"🎉 Chúc mừng bạn đã chọn bộ từ **{deckName}**!")
            .AddEmbed(embed)
            .AddActionRow(buttons)
            .Build();

        await ReplyToChannelWithContentAsync(channelId, content);
    }

    private async Task HandlePollCommandAsync(long channelId, string senderId, string subCommand, ChannelMessageResponse? replyTo = null)
    {
        if (subCommand.StartsWith("vote", StringComparison.OrdinalIgnoreCase))
        {
            await HandlePollVoteCommandAsync(channelId, senderId, subCommand, replyTo);
        }
        else if (subCommand.StartsWith("settle", StringComparison.OrdinalIgnoreCase) ||
                 subCommand.StartsWith("end", StringComparison.OrdinalIgnoreCase))
        {
            var session = ActiveQuizPollSessions.Values
                .Where(s => s.ChannelId == channelId && !s.IsSettled)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            if (session != null)
            {
                await SettleQuizPollAsync(session);
            }
            else
            {
                await ReplyToChannelAsync(channelId, "ℹ️ Hiện không có phiên Clan Quiz Poll nào đang chờ tổng kết trên kênh này.", replyTo);
            }
        }
        else
        {
            await HandlePollQuizCommandAsync(channelId, senderId, subCommand, replyTo);
        }
    }

    public async Task HandlePollQuizCommandAsync(long channelId, string senderId, string query, ChannelMessageResponse? replyTo = null)
    {
        var pollKey = "poll_vocab_meticulous";
        if (!RegisteredClanQuizPolls.TryGetValue(pollKey, out var quizData))
        {
            quizData = RegisteredClanQuizPolls["poll_vocab_meticulous"];
        }

        var content = BuildPollQuizMessage(pollKey);
        var sentMessageId = await ReplyToChannelWithContentAsync(channelId, content, replyTo);

        var sessionKey = $"poll_session_{channelId}_{sentMessageId}";
        var pollSession = new ActiveQuizPollSession(pollKey, channelId, sentMessageId, quizData.CorrectIndex, sentMessageId);
        ActiveQuizPollSessions[sessionKey] = pollSession;

        // Tự động lên lịch giải quyết và cộng XP sau khi poll hết hạn (30 phút)
        var cts = new CancellationTokenSource();
        ActivePollTimers[sessionKey] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), cts.Token);
                if (!pollSession.IsSettled)
                {
                    await SettleQuizPollAsync(pollSession);
                }
            }
            catch (OperationCanceledException)
            {
                // Poll đã được settle thủ công hoặc bot shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [MezonBot] Lỗi khi tự động giải quyết Quiz Poll {SessionKey}", sessionKey);
            }
            finally
            {
                ActivePollTimers.TryRemove(sessionKey, out _);
                cts.Dispose();
            }
        });
    }

    public async Task<PollSettlementResult> SettleQuizPollAsync(
        ActiveQuizPollSession pollSession,
        Mezon.Net.Models.GetPollResponse? injectedResponse = null,
        IServiceScope? externalScope = null)
    {
        lock (pollSession.LockObj)
        {
            if (pollSession.IsSettled)
            {
                return new PollSettlementResult(
                    pollSession.PollKey,
                    pollSession.ChannelId,
                    AlreadySettled: true,
                    TotalVoters: 0,
                    CorrectVotersCount: 0,
                    WrongVotersCount: 0,
                    TotalXpAwarded: 0,
                    AwardedUserIds: new List<string>()
                );
            }
            pollSession.IsSettled = true;
        }

        Mezon.Net.Models.GetPollResponse? pollResult = injectedResponse;
        if (pollResult == null && _client != null)
        {
            try
            {
                var pollParams = new GetPollParams(
                    pollId: pollSession.PollId > 0 ? pollSession.PollId : null,
                    messageId: pollSession.MessageId > 0 ? pollSession.MessageId : null,
                    channelId: pollSession.ChannelId > 0 ? pollSession.ChannelId : null
                );
                pollResult = await _client.GetPollAsync(pollParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gọi SDK GetPollAsync cho poll {PollId}", pollSession.PollId);
            }
        }

        using var scope = externalScope ?? _scopeFactory.CreateScope();
        var userResolver = scope.ServiceProvider.GetRequiredService<IMezonUserResolver>();
        var xpService = scope.ServiceProvider.GetService<IGamificationService>();

        int correctCount = 0;
        int wrongCount = 0;
        int totalXpAwarded = 0;
        var awardedUsers = new List<string>();

        if (pollResult != null)
        {
            var pResp = (Mezon.Net.Models.GetPollResponse)pollResult;
            foreach (var voterDetail in pResp.VoterDetails)
            {
                bool isCorrect = voterDetail.AnswerIndex == pollSession.CorrectIndex;

                foreach (var platformUserId in voterDetail.UserIds)
                {
                    if (platformUserId <= 0) continue;
                    var strUserId = platformUserId.ToString();

                    if (isCorrect)
                    {
                        correctCount++;
                        if (xpService != null)
                        {
                            var internalGuid = await userResolver.ResolveUserIdAsync(strUserId);
                            var xpRequest = new AwardXpRequest(
                                UserId: internalGuid,
                                XpDelta: 10,
                                SourceType: "clan_poll_quiz",
                                IdempotencyKey: $"clan_poll_{pollSession.PollKey}_{internalGuid}"
                            );

                            var xpResult = await xpService.AwardXpAsync(xpRequest);
                            if (xpResult.IsSuccess && !xpResult.AlreadyAwarded)
                            {
                                totalXpAwarded += xpResult.AwardedXp;
                                awardedUsers.Add(strUserId);
                            }
                        }
                    }
                    else
                    {
                        wrongCount++;
                    }
                }
            }
        }

        // Đăng thông báo kết quả vào kênh
        try
        {
            var totalVoters = correctCount + wrongCount;
            var correctLetter = (char)('A' + pollSession.CorrectIndex);
            var embed = new MessageEmbedBuilder()
                .SetColor("#10B981")
                .SetTitle("🏁 KẾT QUẢ ĐỐ VUI CLAN POLL — TỔNG KẾT & TRAO THƯỞNG")
                .SetDescription($"Cuộc thi đố vui Clan đã kết thúc!\n🎯 Đáp án chính xác: Phương án **{correctLetter}**")
                .AddField("📊 Thống kê bình chọn", $"• Tổng lượt tham gia: {totalVoters}\n• Trả lời chính xác: {correctCount} bạn\n• Trả lời chưa đúng: {wrongCount} bạn", false)
                .AddField("⚡ Thưởng XP Server-Side", correctCount > 0
                    ? $"🎉 Đã tự động cộng **+10 XP** cho {correctCount} chiến binh trả lời chính xác!"
                    : "Chưa có bạn nào chọn đúng phương án lần này. Hãy cố gắng ở câu đố sau nhé!", false)
                .SetFooter("LINGUAL Clan Gamification • Mezon Campus Studio 2026", "")
                .Build();

            var content = new MessageContentBuilder()
                .SetText("📢 Tổng kết cuộc thi đố vui Clan Poll:")
                .AddEmbed(embed)
                .Build();

            await ReplyToChannelWithContentAsync(pollSession.ChannelId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi tin nhắn tổng kết Poll settlement");
        }

        return new PollSettlementResult(
            pollSession.PollKey,
            pollSession.ChannelId,
            AlreadySettled: false,
            TotalVoters: correctCount + wrongCount,
            CorrectVotersCount: correctCount,
            WrongVotersCount: wrongCount,
            TotalXpAwarded: totalXpAwarded,
            AwardedUserIds: awardedUsers
        );
    }

    private async Task HandlePollVoteCommandAsync(long channelId, string senderId, string query, ChannelMessageResponse? replyTo = null)
    {
        var content = BuildPollVoteMessage();
        await ReplyToChannelWithContentAsync(channelId, content, replyTo);
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
                var content = BuildRoleplayScenarioSelectContent();
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
    public virtual async Task SendEphemeralMessageAsync(long channelId, long userId, string text)
    {
        SentEphemeralMessages.Add((channelId, userId, text));

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

            ChannelClanMap.TryGetValue(channelId, out var clanId);
            var messageContent = new MessageContentBuilder().SetText(text).Build();
            var innerMessage = new SendChannelMessageParams(
                clanId: clanId,
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
            _logger.LogInformation("🤫 [MezonBot] Đã gửi Ephemeral message (Fallback) tới User {UserId} tại Channel {ChannelId}", userId, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi Ephemeral message tới User {UserId}", userId);
        }
    }

    /// <summary>
    /// Gửi tin nhắn Ephemeral với nội dung đầy đủ (Embed + Buttons) tới người dùng chỉ định.
    /// Sử dụng API chính thức channel.SendEphemeralAsync(content, userId) của Mezon.Net.Sdk.
    /// </summary>
    public virtual async Task SendEphemeralMessageWithContentAsync(long channelId, long userId, MessageContent content)
    {
        SentEphemeralContents.Add((channelId, userId, content));

        if (_client == null || channelId <= 0 || userId <= 0) return;

        try
        {
            var channel = await _client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.SendEphemeralAsync(content, userId);
                _logger.LogInformation("🤫 [MezonBot] Đã gửi Ephemeral Content chuẩn qua channel.SendEphemeralAsync tới User {UserId} tại Channel {ChannelId}", userId, channelId);
                return;
            }

            ChannelClanMap.TryGetValue(channelId, out var clanId);
            var innerMessage = new SendChannelMessageParams(
                clanId: clanId,
                channelId: channelId,
                content: content.ToJson(),
                topicId: null,
                isPublic: false
            );

            var ephParams = new SendEphemeralMessageParams(
                receiverIds: new[] { userId },
                message: innerMessage
            );

            await _client.SendEphemeralMessageToBotAsync(ephParams);
            _logger.LogInformation("🤫 [MezonBot] Đã gửi Ephemeral Content (Fallback) tới User {UserId} tại Channel {ChannelId}", userId, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MezonBot] Lỗi khi gửi Ephemeral Content tới User {UserId}", userId);
        }
    }
}
