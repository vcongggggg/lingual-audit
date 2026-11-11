using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class ReactionQuizService : IReactionQuizService
{
    private readonly MezonBotDbContext _dbContext;
    private readonly IGamificationService? _gamificationService;
    private readonly IMezonUserResolver _userResolver;
    private readonly ILogger<ReactionQuizService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public class InternalOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    public record ReactionQuizUserResponse(
        [property: JsonPropertyName("mezon_user_id")] string MezonUserId,
        [property: JsonPropertyName("user_id")] Guid UserId,
        [property: JsonPropertyName("selected_key")] string SelectedKey,
        [property: JsonPropertyName("is_correct")] bool IsCorrect,
        [property: JsonPropertyName("xp_awarded")] int XpAwarded,
        [property: JsonPropertyName("responded_at")] DateTime RespondedAt
    );

    public ReactionQuizService(
        MezonBotDbContext dbContext,
        IMezonUserResolver userResolver,
        ILogger<ReactionQuizService> logger,
        IGamificationService? gamificationService = null)
    {
        _dbContext = dbContext;
        _userResolver = userResolver;
        _logger = logger;
        _gamificationService = gamificationService;
    }

    public async Task<ReactionQuizPromptDto> CreateQuizAsync(
        long channelId,
        long clanId,
        string? senderId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Đảm bảo seed dữ liệu 10 câu hỏi nếu cơ sở dữ liệu chưa có
        if (!await _dbContext.QuizQuestions.AnyAsync(q => q.Status == "published", cancellationToken))
        {
            var seedList = ReactionQuizSeedData.GetSeedQuestions();
            _dbContext.QuizQuestions.AddRange(seedList);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("🌱 [ReactionQuiz] Đã khởi tạo seed 10 câu hỏi trắc nghiệm tiếng Anh vào CSDL.");
        }

        // 2. Lấy ngẫu nhiên 1 câu hỏi published
        var questions = await _dbContext.QuizQuestions
            .Where(q => q.Status == "published")
            .ToListAsync(cancellationToken);

        var question = questions[Random.Shared.Next(questions.Count)];

        // 3. Phân tích các lựa chọn (bảo mật: không nhúng is_correct ra ngoài DTO hiển thị)
        var parsedOptions = ParseOptions(question.Options);
        var displayOptions = parsedOptions.Select(o => new ReactionQuizOption(o.Key, o.Text)).ToList();

        // 4. Tạo phiên ClanQuizSession trong CSDL
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var session = new ClanQuizSession
        {
            Id = sessionId,
            ChannelMezonId = $"reaction_quiz_pending_{sessionId}",
            QuestionId = question.Id,
            Status = "open",
            OpenedAt = now,
            ClosesAt = now.AddMinutes(10), // Hết hạn sau 10 phút theo yêu cầu
            Responses = "[]"
        };

        _dbContext.ClanQuizSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("🚀 [ReactionQuiz] Đã tạo phiên Quiz {SessionId} tại Channel {ChannelId}. Hết hạn lúc {ClosesAt}",
            sessionId, channelId, session.ClosesAt);

        return new ReactionQuizPromptDto(
            sessionId,
            channelId,
            question.Prompt,
            question.Level,
            question.Difficulty,
            displayOptions,
            session.ClosesAt
        );
    }

    public async Task LinkMessageAsync(Guid sessionId, long messageId, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ClanQuizSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session != null)
        {
            session.ChannelMezonId = $"reaction_quiz_{messageId}";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("🔗 [ReactionQuiz] Đã liên kết phiên {SessionId} với Message {MessageId}", sessionId, messageId);
        }
    }

    public async Task<bool> IsReactionQuizMessageAsync(long messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClanQuizSessions
            .AnyAsync(s => s.ChannelMezonId == $"reaction_quiz_{messageId}", cancellationToken);
    }

    public async Task<ReactionQuizAnswerResult> ProcessReactionAnswerAsync(
        long messageId,
        long channelId,
        string mezonUserId,
        string username,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        // 1. Phân giải emoji sang option key (A, B, C, D)
        var optionKey = ParseQuizEmoji(emoji);
        if (optionKey == null)
        {
            return new ReactionQuizAnswerResult(
                ReactionQuizAnswerStatus.InvalidOption,
                IsCorrect: false,
                XpAwarded: 0,
                CorrectKey: string.Empty,
                Message: "Emoji không phải lựa chọn A/B/C/D hợp lệ."
            );
        }

        // 2. Tra cứu trạng thái phiên Quiz trong CSDL theo messageId
        var session = await _dbContext.ClanQuizSessions
            .Include(s => s.Question)
            .FirstOrDefaultAsync(s => s.ChannelMezonId == $"reaction_quiz_{messageId}", cancellationToken);

        if (session == null)
        {
            // Reaction vào tin nhắn không phải quiz -> bỏ qua hoàn toàn
            return new ReactionQuizAnswerResult(
                ReactionQuizAnswerStatus.NotAQuizMessage,
                IsCorrect: false,
                XpAwarded: 0,
                CorrectKey: string.Empty,
                Message: "Tin nhắn này không phải phiên đố vui trắc nghiệm."
            );
        }

        // 3. Kiểm tra hết hạn (10 phút)
        var now = DateTime.UtcNow;
        if (session.Status != "open" || session.ClosesAt <= now)
        {
            return new ReactionQuizAnswerResult(
                ReactionQuizAnswerStatus.Expired,
                IsCorrect: false,
                XpAwarded: 0,
                CorrectKey: string.Empty,
                Message: "⚠️ LingLing: Quiz này đã hết hạn rồi nhé (thời gian làm bài tối đa 10 phút)!"
            );
        }

        // 4. Kiểm tra user đã trả lời chưa (mỗi user chỉ được tính 1 lần / quiz)
        List<ReactionQuizUserResponse> responses;
        try
        {
            responses = JsonSerializer.Deserialize<List<ReactionQuizUserResponse>>(session.Responses, JsonOptions) ?? new();
        }
        catch
        {
            responses = new();
        }

        if (responses.Any(r => r.MezonUserId == mezonUserId))
        {
            return new ReactionQuizAnswerResult(
                ReactionQuizAnswerStatus.AlreadyAnswered,
                IsCorrect: false,
                XpAwarded: 0,
                CorrectKey: string.Empty,
                Message: "⚠️ Bạn đã chọn đáp án cho câu hỏi này trước đó rồi nhé!"
            );
        }

        // 5. Xác định đáp án đúng lưu ở server
        var options = ParseOptions(session.Question?.Options ?? "[]");
        var correctOption = options.FirstOrDefault(o => o.IsCorrect);
        var correctKey = correctOption?.Key ?? "A";
        var isCorrect = string.Equals(optionKey, correctKey, StringComparison.OrdinalIgnoreCase);

        var internalUserId = await _userResolver.ResolveUserIdAsync(mezonUserId, username, cancellationToken);

        // 6. Chấm điểm server-side: đúng +10 XP, sai 0 XP
        int xpAwarded = 0;
        if (isCorrect && _gamificationService != null && internalUserId != Guid.Empty)
        {
            var xpRequest = new AwardXpRequest(
                UserId: internalUserId,
                XpDelta: 10,
                SourceType: "reaction_quiz",
                IdempotencyKey: $"quiz_{messageId}_{internalUserId}"
            );

            var xpResult = await _gamificationService.AwardXpAsync(xpRequest, cancellationToken);
            if (xpResult != null && xpResult.IsSuccess && !xpResult.AlreadyAwarded)
            {
                xpAwarded = xpResult.AwardedXp;
            }
        }

        // 7. Cập nhật câu trả lời của user vào CSDL
        responses.Add(new ReactionQuizUserResponse(
            MezonUserId: mezonUserId,
            UserId: internalUserId,
            SelectedKey: optionKey,
            IsCorrect: isCorrect,
            XpAwarded: xpAwarded,
            RespondedAt: now
        ));

        session.Responses = JsonSerializer.Serialize(responses, JsonOptions);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("🎯 [ReactionQuiz] User {MezonUserId} chọn {OptionKey} cho Message {MessageId}. Kết quả: {IsCorrect}, +{Xp} XP",
            mezonUserId, optionKey, messageId, isCorrect, xpAwarded);

        if (isCorrect)
        {
            return new ReactionQuizAnswerResult(
                ReactionQuizAnswerStatus.Success,
                IsCorrect: true,
                XpAwarded: xpAwarded,
                CorrectKey: correctKey,
                Message: $"🎉 Chính xác! Đáp án đúng là **{correctKey}**. Bạn được thưởng **+{xpAwarded} XP**!"
            );
        }

        return new ReactionQuizAnswerResult(
            ReactionQuizAnswerStatus.WrongAnswer,
            IsCorrect: false,
            XpAwarded: 0,
            CorrectKey: correctKey,
            Message: "❌ Chưa chính xác rồi! Hãy cố gắng ở câu đố tiếp theo nhé!"
        );
    }

    public static string? ParseQuizEmoji(string? emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji)) return null;
        var e = emoji.Trim();

        // 1. Regional Indicator Symbol flag letters: 🇦 🇧 🇨 🇩
        if (e.Contains("🇦") || e.Contains("\uD83C\uDDE6") || e.Contains("regional_indicator_a") || e.Contains("flag_a") || e.Equals("🅰️", StringComparison.OrdinalIgnoreCase) || e.Equals("A", StringComparison.OrdinalIgnoreCase))
        {
            return "A";
        }
        if (e.Contains("🇧") || e.Contains("\uD83C\uDDE7") || e.Contains("regional_indicator_b") || e.Contains("flag_b") || e.Equals("🅱️", StringComparison.OrdinalIgnoreCase) || e.Equals("B", StringComparison.OrdinalIgnoreCase))
        {
            return "B";
        }
        if (e.Contains("🇨") || e.Contains("\uD83C\uDDE8") || e.Contains("regional_indicator_c") || e.Contains("flag_c") || e.Equals("🅲", StringComparison.OrdinalIgnoreCase) || e.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            return "C";
        }
        if (e.Contains("🇩") || e.Contains("\uD83C\uDDE9") || e.Contains("regional_indicator_d") || e.Contains("flag_d") || e.Equals("🅳", StringComparison.OrdinalIgnoreCase) || e.Equals("D", StringComparison.OrdinalIgnoreCase))
        {
            return "D";
        }

        return null;
    }

    private static List<InternalOption> ParseOptions(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return new List<InternalOption>();
        try
        {
            return JsonSerializer.Deserialize<List<InternalOption>>(rawJson, JsonOptions) ?? new List<InternalOption>();
        }
        catch
        {
            return new List<InternalOption>();
        }
    }
}

