using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class WordDuelService : IWordDuelService
{
    public static readonly Guid LingLingBotUserId = Guid.Parse("00000000-0000-0000-0000-00000000bb07");
    public static readonly Guid OpenChallengePlaceholderUserId = Guid.Parse("00000000-0000-0000-0000-00000000cc01");

    public const int WinnerXpReward = 30;
    public const int DrawXpReward = 15;
    public const int ParticipantXpReward = 10;
    public const int MaxRoundDurationMs = 10000;

    private readonly GamificationDbContext _dbContext;
    private readonly IXpService _xpService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WordDuelService> _logger;

    private static readonly SemaphoreSlim MatchMutationLock = new(1, 1);
    private static readonly ConcurrentDictionary<Guid, DuelMatchRuntimeMeta> MatchMetaStore = new();
    private static readonly ConcurrentDictionary<Guid, InternalQuestionModel> QuestionCache = new();
    private static readonly ConcurrentDictionary<Guid, bool> PublishedHonorCards = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class DuelMatchRuntimeMeta
    {
        public string ChallengerName { get; set; } = "Challenger";
        public string OpponentName { get; set; } = "Opponent";
        public long? ClanChannelId { get; set; }
        public int CurrentRound { get; set; } = 1;
    }

    private sealed record InternalOptionModel(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("is_correct")] bool IsCorrect
    );

    private sealed record InternalQuestionModel(
        Guid Id,
        string Prompt,
        string Level,
        int Difficulty,
        List<InternalOptionModel> Options,
        string Explanation
    );

    private static readonly List<InternalQuestionModel> CuratedDuelQuestions = new()
    {
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000001"),
            "Choose the best word: Passengers must show their boarding pass at the departure ___.",
            "A2",
            2,
            new List<InternalOptionModel>
            {
                new("A", "gate", true),
                new("B", "kitchen", false),
                new("C", "pillow", false),
                new("D", "garden", false)
            },
            "'Departure gate' (cửa khởi hành) là cụm danh từ chuẩn tại sân bay nơi hành khách xuất trình thẻ lên máy bay."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000002"),
            "Which word means 'able to speak a language easily and accurately'?",
            "B1",
            2,
            new List<InternalOptionModel>
            {
                new("A", "hesitant", false),
                new("B", "fluent", true),
                new("C", "silent", false),
                new("D", "fragile", false)
            },
            "'Fluent' (trôi chảy, thông thạo) mô tả khả năng giao tiếp ngôn ngữ tự nhiên và chính xác."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000003"),
            "Complete the sentence: She ___ in London for three years before moving to Tokyo.",
            "B1",
            3,
            new List<InternalOptionModel>
            {
                new("A", "lives", false),
                new("B", "is living", false),
                new("C", "had lived", true),
                new("D", "will live", false)
            },
            "Thì quá khứ hoàn thành (had + V3/ed) diễn tả một hành động đã xảy ra và kéo dài trước một mốc trong quá khứ (before moving)."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000004"),
            "What is the closest synonym of 'essential' in: 'Teamwork is essential for project success'?",
            "A2",
            2,
            new List<InternalOptionModel>
            {
                new("A", "crucial", true),
                new("B", "optional", false),
                new("C", "trivial", false),
                new("D", "accidental", false)
            },
            "'Crucial' và 'essential' đều mang nghĩa 'cực kỳ quan trọng, thiết yếu', trái nghĩa với 'optional' và 'trivial'."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000005"),
            "Choose the correct preposition: Our engineering team is responsible ___ maintaining the real-time server.",
            "A2",
            2,
            new List<InternalOptionModel>
            {
                new("A", "with", false),
                new("B", "for", true),
                new("C", "at", false),
                new("D", "about", false)
            },
            "Cấu trúc cố định 'be responsible for + V-ing/Noun' nghĩa là chịu trách nhiệm về việc gì."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000006"),
            "Which word best fits: Could you please send me the meeting ___ so I can prepare the slides?",
            "A2",
            2,
            new List<InternalOptionModel>
            {
                new("A", "agenda", true),
                new("B", "receipt", false),
                new("C", "dessert", false),
                new("D", "luggage", false)
            },
            "'Meeting agenda' là chương trình nghị sự / nội dung cuộc họp cần chuẩn bị trước."
        ),
        new InternalQuestionModel(
            Guid.Parse("d0000000-0000-0000-0000-000000000007"),
            "Identify the correct conditional form: If I ___ more time today, I would practice another Word Duel.",
            "B1",
            3,
            new List<InternalOptionModel>
            {
                new("A", "have", false),
                new("B", "had", true),
                new("C", "will have", false),
                new("D", "having", false)
            },
            "Câu điều kiện loại 2 (giả định trái với hiện tại) dùng cấu trúc: If + S + V2/ed (had), S + would + V-inf."
        )
    };

    static WordDuelService()
    {
        foreach (var q in CuratedDuelQuestions)
        {
            QuestionCache[q.Id] = q;
        }
    }

    public WordDuelService(
        GamificationDbContext dbContext,
        IXpService xpService,
        IServiceProvider serviceProvider,
        ILogger<WordDuelService> logger)
    {
        _dbContext = dbContext;
        _xpService = xpService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public static decimal CalculateRoundScore(bool isCorrect, int responseMs)
    {
        if (!isCorrect) return 0m;
        var clampedMs = Math.Clamp(responseMs, 0, MaxRoundDurationMs);
        var speedBonus = Math.Round((MaxRoundDurationMs - clampedMs) / 100.0m, 2);
        return 100.0m + speedBonus;
    }

    public async Task<DuelMatchDto> CreateChallengeAsync(
        CreateDuelChallengeRequest request,
        CancellationToken cancellationToken = default)
    {
        var questionCount = Math.Clamp(request.QuestionCount, (short)1, (short)10);
        var challengerId = request.ChallengerId == Guid.Empty ? Guid.NewGuid() : request.ChallengerId;
        var opponentId = request.OpponentId;

        if (opponentId == Guid.Empty || opponentId == challengerId)
        {
            opponentId = request.AutoAccept ? LingLingBotUserId : OpenChallengePlaceholderUserId;
        }

        var challengerName = string.IsNullOrWhiteSpace(request.ChallengerName) ? "Challenger" : request.ChallengerName.Trim();
        var opponentName = opponentId == LingLingBotUserId
            ? "LingLing AI"
            : (string.IsNullOrWhiteSpace(request.OpponentName) ? "Opponent" : request.OpponentName.Trim());

        await EnsureUsersExistInRelationalDbAsync(challengerId, challengerName, opponentId, opponentName, cancellationToken);
        var selectedQuestions = await SelectMatchQuestionsAsync(questionCount, cancellationToken);
        var questionIdList = selectedQuestions.Select(q => q.Id.ToString()).ToList();

        var now = DateTime.UtcNow;
        var isAccepted = request.AutoAccept || opponentId == LingLingBotUserId;

        var match = new DuelMatch
        {
            Id = Guid.NewGuid(),
            ChallengerId = challengerId,
            OpponentId = opponentId,
            Status = isAccepted ? "accepted" : "pending",
            QuestionCount = questionCount,
            QuestionIds = JsonSerializer.Serialize(questionIdList),
            ChallengerAnswers = "[]",
            OpponentAnswers = "[]",
            ChallengerScore = 0m,
            OpponentScore = 0m,
            ChallengerCorrect = 0,
            OpponentCorrect = 0,
            StartedAt = isAccepted ? now : null,
            CreatedAt = now
        };

        _dbContext.DuelMatches.Add(match);
        await _dbContext.SaveChangesAsync(cancellationToken);

        MatchMetaStore[match.Id] = new DuelMatchRuntimeMeta
        {
            ChallengerName = challengerName,
            OpponentName = opponentName,
            ClanChannelId = request.ClanChannelId,
            CurrentRound = 1
        };

        _logger.LogInformation("⚔️ [WordDuel] Khởi tạo trận đấu {MatchId}: {Challenger} vs {Opponent} (Status: {Status})",
            match.Id, challengerName, opponentName, match.Status);

        return MapToDto(match);
    }

    public async Task<DuelMatchDto?> AcceptChallengeAsync(
        Guid matchId,
        Guid acceptorId,
        string? acceptorName = null,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        if (match.Status is "completed" or "declined" or "expired" or "cancelled")
        {
            return MapToDto(match);
        }

        // Nếu là thách đấu mở (OpenChallengePlaceholderUserId) hoặc thách đấu đúng đối thủ
        if (acceptorId != Guid.Empty && acceptorId != match.ChallengerId)
        {
            var resolvedOpponentName = string.IsNullOrWhiteSpace(acceptorName) ? "Opponent" : acceptorName.Trim();
            await EnsureUsersExistInRelationalDbAsync(match.ChallengerId, "Challenger", acceptorId, resolvedOpponentName, cancellationToken);
            match.OpponentId = acceptorId;

            if (MatchMetaStore.TryGetValue(matchId, out var meta))
            {
                meta.OpponentName = resolvedOpponentName;
            }
            else
            {
                MatchMetaStore[matchId] = new DuelMatchRuntimeMeta
                {
                    ChallengerName = "Challenger",
                    OpponentName = resolvedOpponentName
                };
            }
        }

        match.Status = "accepted";
        match.StartedAt ??= DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [WordDuel] Trận đấu {MatchId} đã chuyển sang trạng thái 'accepted' bởi {AcceptorId}", matchId, acceptorId);
        return MapToDto(match);
    }

    public async Task<DuelMatchDto?> DeclineChallengeAsync(
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        if (match.Status is "pending" or "accepted")
        {
            match.Status = userId == match.ChallengerId ? "cancelled" : "declined";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("🚫 [WordDuel] Trận đấu {MatchId} đã bị hủy/từ chối ({Status}) bởi {UserId}", matchId, match.Status, userId);
        }

        return MapToDto(match);
    }

    public async Task<DuelMatchDto?> ExpireNoShowMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        if (match.Status is "pending" or "accepted")
        {
            match.Status = "expired";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("⏰ [WordDuel] Trận đấu {MatchId} đã hết hạn chờ (expired / no_show).", matchId);
        }

        return MapToDto(match);
    }

    public async Task<DuelMatchDto?> StartMatchInProgressAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        if (match.Status is "pending" or "accepted")
        {
            match.Status = "in_progress";
            match.StartedAt ??= DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("🔥 [WordDuel] Trận đấu {MatchId} bắt đầu thi đấu ('in_progress').", matchId);
        }

        return MapToDto(match);
    }

    public async Task<DuelRoundQuestionDto?> GetRoundQuestionAsync(
        Guid matchId,
        int roundNumber,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        var questionIds = ParseQuestionIds(match.QuestionIds);
        if (roundNumber < 1 || roundNumber > questionIds.Count) return null;

        var targetQId = questionIds[roundNumber - 1];
        var question = await GetQuestionModelByIdAsync(targetQId, cancellationToken);
        if (question == null) return null;

        if (MatchMetaStore.TryGetValue(matchId, out var meta))
        {
            meta.CurrentRound = roundNumber;
        }

        return new DuelRoundQuestionDto(
            Id: question.Id,
            RoundNumber: roundNumber,
            TotalRounds: questionIds.Count,
            Prompt: question.Prompt,
            Level: question.Level,
            Difficulty: question.Difficulty,
            Options: question.Options.Select(o => new DuelOptionDto(o.Key, o.Text)).ToList(),
            DurationSeconds: 10
        );
    }

    public async Task<SubmitDuelAnswerResultDto> SubmitAnswerAsync(
        SubmitDuelAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        await MatchMutationLock.WaitAsync(cancellationToken);
        try
        {
            var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
            if (match == null)
            {
                return new SubmitDuelAnswerResultDto(false, "Không tìm thấy trận đấu.", 0, false, request.SelectedKey, "", "", "", 0m, 0m, 0m, 0, false, false, null);
            }

            if (match.Status is "completed" or "declined" or "expired" or "cancelled")
            {
                return new SubmitDuelAnswerResultDto(false, "Trận đấu này đã kết thúc.", 0, false, request.SelectedKey, "", "", "", 0m, match.ChallengerScore, match.OpponentScore, 0, false, true, MapToDto(match));
            }

            if (match.Status is "pending" or "accepted")
            {
                match.Status = "in_progress";
                match.StartedAt ??= DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var isChallenger = request.UserId == match.ChallengerId;
            var isOpponent = request.UserId == match.OpponentId;
            if (!isChallenger && !isOpponent)
            {
                return new SubmitDuelAnswerResultDto(false, "Người chơi không thuộc trận đấu này.", 0, false, request.SelectedKey, "", "", "", 0m, 0m, 0m, 0, false, false, null);
            }

            var questionIds = ParseQuestionIds(match.QuestionIds);
            var roundIndex = questionIds.IndexOf(request.QuestionId);
            if (roundIndex < 0)
            {
                return new SubmitDuelAnswerResultDto(false, "Câu hỏi không thuộc trận đấu này.", 0, false, request.SelectedKey, "", "", "", 0m, 0m, 0m, 0, false, false, null);
            }

            var roundNumber = roundIndex + 1;
            var question = await GetQuestionModelByIdAsync(request.QuestionId, cancellationToken);
            if (question == null)
            {
                return new SubmitDuelAnswerResultDto(false, "Không tìm thấy dữ liệu câu hỏi.", roundNumber, false, request.SelectedKey, "", "", "", 0m, 0m, 0m, 0, false, false, null);
            }

            var existingChallengerAnswers = ParseAnswers(match.ChallengerAnswers);
            var existingOpponentAnswers = ParseAnswers(match.OpponentAnswers);
            var playerAnswers = isChallenger ? existingChallengerAnswers : existingOpponentAnswers;

            if (playerAnswers.Any(a => a.QuestionId == request.QuestionId))
            {
                var correctOptDup = question.Options.FirstOrDefault(o => o.IsCorrect);
                return new SubmitDuelAnswerResultDto(
                    false,
                    "Bạn đã gửi đáp án cho vòng này rồi!",
                    roundNumber,
                    false,
                    request.SelectedKey,
                    correctOptDup?.Key ?? "A",
                    correctOptDup?.Text ?? "",
                    question.Explanation,
                    0m,
                    isChallenger ? match.ChallengerScore : match.OpponentScore,
                    isChallenger ? match.OpponentScore : match.ChallengerScore,
                    CalculateComboStreak(playerAnswers),
                    existingChallengerAnswers.Any(a => a.QuestionId == request.QuestionId) && existingOpponentAnswers.Any(a => a.QuestionId == request.QuestionId),
                    match.Status == "completed",
                    MapToDto(match));
            }

            var cleanKey = (request.SelectedKey ?? string.Empty).Trim().ToUpperInvariant();
            var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect) ?? question.Options.First();
            var isCorrect = !string.IsNullOrEmpty(cleanKey) &&
                            cleanKey != "TIMEOUT" &&
                            correctOption.Key.Equals(cleanKey, StringComparison.OrdinalIgnoreCase);

            var clampedMs = Math.Clamp(request.ResponseMs, 0, MaxRoundDurationMs);
            var pointsEarned = CalculateRoundScore(isCorrect, clampedMs);

            var newAnswerEntry = new DuelAnswerEntryDto(
                QuestionId: request.QuestionId,
                Round: roundNumber,
                SelectedKey: string.IsNullOrEmpty(cleanKey) ? "TIMEOUT" : cleanKey,
                IsCorrect: isCorrect,
                ResponseMs: clampedMs,
                Score: pointsEarned,
                AnsweredAt: DateTime.UtcNow
            );

            // SP3-04: Tuân thủ nghiêm ngặt quy tắc Concurrency JSONB Atomic Append
            var singleElementArrayJson = JsonSerializer.Serialize(new[] { newAnswerEntry }, JsonOptions);
            short correctDelta = isCorrect ? (short)1 : (short)0;

            if (_dbContext.Database.IsRelational())
            {
                if (isChallenger)
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE duel_matches
                        SET challenger_answers = challenger_answers || {singleElementArrayJson}::jsonb,
                            challenger_score = challenger_score + {pointsEarned},
                            challenger_correct = challenger_correct + {correctDelta}
                        WHERE id = {match.Id} AND status IN ('accepted', 'in_progress');",
                        cancellationToken);
                }
                else
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE duel_matches
                        SET opponent_answers = opponent_answers || {singleElementArrayJson}::jsonb,
                            opponent_score = opponent_score + {pointsEarned},
                            opponent_correct = opponent_correct + {correctDelta}
                        WHERE id = {match.Id} AND status IN ('accepted', 'in_progress');",
                        cancellationToken);
                }

                await _dbContext.Entry(match).ReloadAsync(cancellationToken);
            }
            else
            {
                playerAnswers.Add(newAnswerEntry);
                if (isChallenger)
                {
                    match.ChallengerAnswers = JsonSerializer.Serialize(playerAnswers, JsonOptions);
                    match.ChallengerScore += pointsEarned;
                    match.ChallengerCorrect += correctDelta;
                }
                else
                {
                    match.OpponentAnswers = JsonSerializer.Serialize(playerAnswers, JsonOptions);
                    match.OpponentScore += pointsEarned;
                    match.OpponentCorrect += correctDelta;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var updatedChallengerAnswers = ParseAnswers(match.ChallengerAnswers);
            var updatedOpponentAnswers = ParseAnswers(match.OpponentAnswers);
            var updatedPlayerAnswers = isChallenger ? updatedChallengerAnswers : updatedOpponentAnswers;

            var bothAnsweredRound = updatedChallengerAnswers.Any(a => a.QuestionId == request.QuestionId) &&
                                    updatedOpponentAnswers.Any(a => a.QuestionId == request.QuestionId);

            var allRoundsFinished = updatedChallengerAnswers.Count >= match.QuestionCount &&
                                    updatedOpponentAnswers.Count >= match.QuestionCount;

            DuelMatchDto? finalSnapshot = null;
            if (allRoundsFinished && match.Status != "completed")
            {
                finalSnapshot = await FinalizeMatchInternalAsync(match, cancellationToken);
            }
            else
            {
                finalSnapshot = MapToDto(match);
            }

            var playerScore = isChallenger ? match.ChallengerScore : match.OpponentScore;
            var opponentScore = isChallenger ? match.OpponentScore : match.ChallengerScore;
            var combo = CalculateComboStreak(updatedPlayerAnswers);

            return new SubmitDuelAnswerResultDto(
                Accepted: true,
                Message: isCorrect ? $"Chính xác! +{pointsEarned:F0} điểm" : "Chưa chính xác!",
                RoundNumber: roundNumber,
                IsCorrect: isCorrect,
                SelectedKey: newAnswerEntry.SelectedKey,
                CorrectKey: correctOption.Key,
                CorrectText: correctOption.Text,
                Explanation: question.Explanation,
                PointsEarned: pointsEarned,
                PlayerTotalScore: playerScore,
                OpponentTotalScore: opponentScore,
                PlayerComboStreak: combo,
                BothAnsweredCurrentRound: bothAnsweredRound,
                MatchCompleted: match.Status == "completed",
                MatchSnapshot: finalSnapshot
            );
        }
        finally
        {
            MatchMutationLock.Release();
        }
    }

    public async Task<DuelMatchDto?> ForfeitMatchAsync(
        Guid matchId,
        Guid forfeitingUserId,
        string reason = "forfeit",
        CancellationToken cancellationToken = default)
    {
        await MatchMutationLock.WaitAsync(cancellationToken);
        try
        {
            var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
            if (match == null) return null;

            if (match.Status == "completed")
            {
                return await BuildDetailedMatchDtoAsync(match, cancellationToken);
            }

            var winnerId = forfeitingUserId == match.ChallengerId ? match.OpponentId : match.ChallengerId;
            match.StartedAt ??= DateTime.UtcNow;
            match.WinnerId = winnerId;
            match.Status = "completed";
            match.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("🏳️ [WordDuel] Trận đấu {MatchId} kết thúc do {UserId} bỏ cuộc/mất kết nối ({Reason}). Người thắng: {WinnerId}",
                matchId, forfeitingUserId, reason, winnerId);

            return await AwardDuelXpAndNotifyClanAsync(match, cancellationToken);
        }
        finally
        {
            MatchMutationLock.Release();
        }
    }

    public async Task<DuelMatchDto?> CompleteMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        await MatchMutationLock.WaitAsync(cancellationToken);
        try
        {
            var match = await _dbContext.DuelMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
            if (match == null) return null;

            if (match.Status == "completed")
            {
                return await BuildDetailedMatchDtoAsync(match, cancellationToken);
            }

            return await FinalizeMatchInternalAsync(match, cancellationToken);
        }
        finally
        {
            MatchMutationLock.Release();
        }
    }

    public async Task<DuelMatchDto?> GetMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.DuelMatches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
        if (match == null) return null;

        if (match.Status == "completed")
        {
            return await BuildDetailedMatchDtoAsync(match, cancellationToken);
        }

        return MapToDto(match);
    }

    public async Task<IReadOnlyList<DuelMatchDto>> GetUserMatchHistoryAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var query = _dbContext.DuelMatches.AsNoTracking();

        if (userId != Guid.Empty)
        {
            query = query.Where(m => m.ChallengerId == userId || m.OpponentId == userId);
        }

        var matches = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(cancellationToken);

        return matches.Select(m => MapToDto(m)).ToList();
    }

    private async Task<DuelMatchDto> FinalizeMatchInternalAsync(
        DuelMatch match,
        CancellationToken cancellationToken)
    {
        match.StartedAt ??= DateTime.UtcNow;
        match.CompletedAt = DateTime.UtcNow;
        match.Status = "completed";

        if (match.ChallengerScore > match.OpponentScore)
        {
            match.WinnerId = match.ChallengerId;
        }
        else if (match.OpponentScore > match.ChallengerScore)
        {
            match.WinnerId = match.OpponentId;
        }
        else
        {
            match.WinnerId = null; // Hòa (Draw)
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("🏆 [WordDuel] Hoàn tất trận đấu {MatchId}: Challenger={CScore} vs Opponent={OScore}, Winner={WinnerId}",
            match.Id, match.ChallengerScore, match.OpponentScore, match.WinnerId);

        return await AwardDuelXpAndNotifyClanAsync(match, cancellationToken);
    }

    private async Task<DuelMatchDto> AwardDuelXpAndNotifyClanAsync(
        DuelMatch match,
        CancellationToken cancellationToken)
    {
        var (challengerXp, opponentXp) = CalculateMatchXpAwards(match);

        // Cộng XP vào xp_ledger, cập nhật Streak và BXH Redis cho Challenger
        if (match.ChallengerId != Guid.Empty &&
            match.ChallengerId != LingLingBotUserId &&
            match.ChallengerId != OpenChallengePlaceholderUserId)
        {
            try
            {
                await _xpService.AwardXpAsync(new AwardXpRequest(
                    UserId: match.ChallengerId,
                    XpDelta: challengerXp,
                    SourceType: match.WinnerId == match.ChallengerId ? "word_duel_win" : "word_duel_participation",
                    IdempotencyKey: $"duel_xp_{match.Id}_{match.ChallengerId}",
                    SourceId: match.Id,
                    Metadata: JsonSerializer.Serialize(new
                    {
                        match_id = match.Id,
                        role = "challenger",
                        score = match.ChallengerScore,
                        is_winner = match.WinnerId == match.ChallengerId
                    })
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [WordDuel] Không thể cộng XP cho Challenger {UserId} trong trận {MatchId}.", match.ChallengerId, match.Id);
            }
        }

        // Cộng XP vào xp_ledger, cập nhật Streak và BXH Redis cho Opponent (nếu là người thật)
        if (match.OpponentId != Guid.Empty &&
            match.OpponentId != LingLingBotUserId &&
            match.OpponentId != OpenChallengePlaceholderUserId)
        {
            try
            {
                await _xpService.AwardXpAsync(new AwardXpRequest(
                    UserId: match.OpponentId,
                    XpDelta: opponentXp,
                    SourceType: match.WinnerId == match.OpponentId ? "word_duel_win" : "word_duel_participation",
                    IdempotencyKey: $"duel_xp_{match.Id}_{match.OpponentId}",
                    SourceId: match.Id,
                    Metadata: JsonSerializer.Serialize(new
                    {
                        match_id = match.Id,
                        role = "opponent",
                        score = match.OpponentScore,
                        is_winner = match.WinnerId == match.OpponentId
                    })
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [WordDuel] Không thể cộng XP cho Opponent {UserId} trong trận {MatchId}.", match.OpponentId, match.Id);
            }
        }

        var detailedDto = await BuildDetailedMatchDtoAsync(match, cancellationToken);

        // SP3-04: Bắn Thẻ vinh danh kết quả ra kênh Clan (Idempotent — chống đăng trùng)
        if (PublishedHonorCards.TryAdd(match.Id, true))
        {
            var clanNotifier = _serviceProvider.GetService<IDuelClanNotifier>();
            if (clanNotifier != null)
            {
                try
                {
                    await clanNotifier.PublishMatchHonorCardAsync(detailedDto, detailedDto.ClanChannelId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [WordDuel] Không thể gửi Thẻ vinh danh Clan cho trận {MatchId}.", match.Id);
                }
            }
        }

        return detailedDto;
    }

    private static (int ChallengerXp, int OpponentXp) CalculateMatchXpAwards(DuelMatch match)
    {
        if (match.WinnerId == match.ChallengerId)
        {
            return (WinnerXpReward, ParticipantXpReward);
        }

        if (match.WinnerId == match.OpponentId)
        {
            return (ParticipantXpReward, WinnerXpReward);
        }

        return (DrawXpReward, DrawXpReward);
    }

    private async Task<DuelMatchDto> BuildDetailedMatchDtoAsync(
        DuelMatch match,
        CancellationToken cancellationToken)
    {
        var questionIds = ParseQuestionIds(match.QuestionIds);
        var challengerAnswers = ParseAnswers(match.ChallengerAnswers);
        var opponentAnswers = ParseAnswers(match.OpponentAnswers);

        var reviews = new List<DuelQuestionReviewDto>();
        for (var i = 0; i < questionIds.Count; i++)
        {
            var qId = questionIds[i];
            var qModel = await GetQuestionModelByIdAsync(qId, cancellationToken);
            if (qModel == null) continue;

            var cAns = challengerAnswers.FirstOrDefault(a => a.QuestionId == qId);
            var oAns = opponentAnswers.FirstOrDefault(a => a.QuestionId == qId);
            var correctOpt = qModel.Options.FirstOrDefault(o => o.IsCorrect) ?? qModel.Options.First();

            reviews.Add(new DuelQuestionReviewDto(
                QuestionId: qId,
                RoundNumber: i + 1,
                Prompt: qModel.Prompt,
                Level: qModel.Level,
                Options: qModel.Options.Select(o => new DuelOptionDto(o.Key, o.Text)).ToList(),
                CorrectKey: correctOpt.Key,
                CorrectText: correctOpt.Text,
                Explanation: qModel.Explanation,
                ChallengerSelectedKey: cAns?.SelectedKey,
                ChallengerCorrect: cAns?.IsCorrect ?? false,
                ChallengerResponseMs: cAns?.ResponseMs ?? MaxRoundDurationMs,
                ChallengerRoundScore: cAns?.Score ?? 0m,
                OpponentSelectedKey: oAns?.SelectedKey,
                OpponentCorrect: oAns?.IsCorrect ?? false,
                OpponentResponseMs: oAns?.ResponseMs ?? MaxRoundDurationMs,
                OpponentRoundScore: oAns?.Score ?? 0m
            ));
        }

        return MapToDto(match, reviews);
    }

    private DuelMatchDto MapToDto(DuelMatch match, IReadOnlyList<DuelQuestionReviewDto>? reviews = null)
    {
        MatchMetaStore.TryGetValue(match.Id, out var meta);
        var challengerName = meta?.ChallengerName ?? $"Player_{match.ChallengerId.ToString()[..6]}";
        var opponentName = match.OpponentId == LingLingBotUserId
            ? "LingLing AI"
            : (meta?.OpponentName ?? $"Player_{match.OpponentId.ToString()[..6]}");

        var (challengerXp, opponentXp) = match.Status == "completed"
            ? CalculateMatchXpAwards(match)
            : (0, 0);

        return new DuelMatchDto(
            Id: match.Id,
            ChallengerId: match.ChallengerId,
            ChallengerName: challengerName,
            OpponentId: match.OpponentId,
            OpponentName: opponentName,
            Status: match.Status,
            QuestionCount: match.QuestionCount,
            ChallengerScore: match.ChallengerScore,
            OpponentScore: match.OpponentScore,
            ChallengerCorrect: match.ChallengerCorrect,
            OpponentCorrect: match.OpponentCorrect,
            WinnerId: match.WinnerId,
            ChallengerXpAwarded: challengerXp,
            OpponentXpAwarded: opponentXp,
            CreatedAt: match.CreatedAt,
            StartedAt: match.StartedAt,
            CompletedAt: match.CompletedAt,
            CurrentRound: meta?.CurrentRound ?? 1,
            IsBotMatch: match.OpponentId == LingLingBotUserId,
            ClanChannelId: meta?.ClanChannelId,
            QuestionReviews: reviews
        );
    }

    private static int CalculateComboStreak(List<DuelAnswerEntryDto> answers)
    {
        var combo = 0;
        foreach (var ans in answers.OrderBy(a => a.Round))
        {
            if (ans.IsCorrect) combo++;
            else combo = 0;
        }
        return combo;
    }

    private static List<Guid> ParseQuestionIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<Guid>();
        try
        {
            var raw = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (raw == null) return new List<Guid>();
            var list = new List<Guid>();
            foreach (var item in raw)
            {
                if (Guid.TryParse(item, out var g)) list.Add(g);
            }
            return list;
        }
        catch
        {
            return new List<Guid>();
        }
    }

    private static List<DuelAnswerEntryDto> ParseAnswers(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<DuelAnswerEntryDto>();
        try
        {
            return JsonSerializer.Deserialize<List<DuelAnswerEntryDto>>(json, JsonOptions) ?? new List<DuelAnswerEntryDto>();
        }
        catch
        {
            return new List<DuelAnswerEntryDto>();
        }
    }

    private async Task<List<InternalQuestionModel>> SelectMatchQuestionsAsync(
        int count,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsRelational())
        {
            try
            {
                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                // Kiểm tra và nạp các câu hỏi chuẩn vào quiz_questions để thỏa mãn trigger trg_duel_question_ref_check
                foreach (var q in CuratedDuelQuestions)
                {
                    var optionsJson = JsonSerializer.Serialize(q.Options, JsonOptions);
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO quiz_questions (id, question_type, prompt, difficulty, level, options, payload, explanation, points, sort_order, status)
                        VALUES ({q.Id}, 'multiple_choice', {q.Prompt}, {(short)q.Difficulty}, {q.Level}, {optionsJson}::jsonb, '{{}}'::jsonb, {q.Explanation}, 1, 1, 'published')
                        ON CONFLICT (id) DO NOTHING;",
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Không thể seed câu hỏi dự phòng vào bảng quiz_questions.");
            }
        }

        return CuratedDuelQuestions
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToList();
    }

    private Task<InternalQuestionModel?> GetQuestionModelByIdAsync(Guid questionId, CancellationToken cancellationToken)
    {
        if (QuestionCache.TryGetValue(questionId, out var cached))
        {
            return Task.FromResult<InternalQuestionModel?>(cached);
        }

        var fallback = CuratedDuelQuestions.FirstOrDefault(q => q.Id == questionId) ?? CuratedDuelQuestions[0];
        return Task.FromResult<InternalQuestionModel?>(fallback);
    }

    private async Task EnsureUsersExistInRelationalDbAsync(
        Guid challengerId,
        string challengerName,
        Guid opponentId,
        string opponentName,
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational()) return;

        try
        {
            var cMezonId = $"duel_{challengerId:N}"[..24];
            var oMezonId = $"duel_{opponentId:N}"[..24];

            await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO users (id, mezon_user_id, username, display_name, role, status)
                VALUES ({challengerId}, {cMezonId}, {challengerName}, {challengerName}, 'learner', 'active')
                ON CONFLICT (id) DO NOTHING;",
                cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO users (id, mezon_user_id, username, display_name, role, status)
                VALUES ({opponentId}, {oMezonId}, {opponentName}, {opponentName}, 'learner', 'active')
                ON CONFLICT (id) DO NOTHING;",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Bỏ qua upsert users cho duel_matches.");
        }
    }
}
