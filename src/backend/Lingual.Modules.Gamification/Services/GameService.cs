using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class GameService : IGameService
{
    private static readonly HashSet<string> ValidGameTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "word_match",
        "sentence_scramble",
        "typing_race",
        "fill_blitz",
        "sound_reflex",
        "lingo_wordle"
    };

    private readonly GamificationDbContext _dbContext;
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<GameService> _logger;

    public GameService(
        GamificationDbContext dbContext,
        IGamificationService gamificationService,
        ILogger<GameService> logger)
    {
        _dbContext = dbContext;
        _gamificationService = gamificationService;
        _logger = logger;
    }

    public async Task<GameDataResponseDto> GetGameDataAsync(
        string? type,
        string? difficulty,
        Guid userId,
        CancellationToken ct = default)
    {
        var gameType = (type ?? "word_match").Trim().ToLowerInvariant();
        if (!ValidGameTypes.Contains(gameType))
        {
            throw new ArgumentException($"Loại trò chơi '{gameType}' không hợp lệ.");
        }

        var attemptId = Guid.NewGuid().ToString("N");
        object responsePayload;
        string serverTruthJson;

        switch (gameType)
        {
            case "word_match":
            {
                var query = _dbContext.GameWordPools.AsQueryable();
                if (!string.IsNullOrWhiteSpace(difficulty) && !difficulty.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(w => w.CefrLevel.ToLower() == difficulty.ToLower());
                }

                var words = await query
                    .OrderBy(w => EF.Functions.Random())
                    .Take(8)
                    .ToListAsync(ct);

                if (words.Count < 4)
                {
                    words = await _dbContext.GameWordPools
                        .Take(8)
                        .ToListAsync(ct);
                }

                var items = words.Select(w => new
                {
                    id = w.Id,
                    targetText = w.TargetText,
                    translation = w.Translation,
                    phonetic = w.Phonetic,
                    cefrLevel = w.CefrLevel
                }).ToList();

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    items
                };

                serverTruthJson = JsonSerializer.Serialize(items.Select(i => new { id = i.id, targetText = i.targetText, translation = i.translation }));
                break;
            }

            case "sentence_scramble":
            {
                var questions = await _dbContext.GameQuestions
                    .Where(q => q.GameType == "sentence_scramble")
                    .OrderBy(q => EF.Functions.Random())
                    .Take(12)
                    .ToListAsync(ct);

                if (questions.Count == 0)
                {
                    throw new KeyNotFoundException("Chưa có dữ liệu câu hỏi cho game xếp câu.");
                }

                var clientQuestions = questions.Select(q =>
                {
                    List<string> tokens;
                    try
                    {
                        tokens = !string.IsNullOrWhiteSpace(q.ChoicesJson)
                            ? JsonSerializer.Deserialize<List<string>>(q.ChoicesJson) ?? new List<string>()
                            : q.Answer.Split(' ').OrderBy(_ => Guid.NewGuid()).ToList();
                    }
                    catch
                    {
                        tokens = q.Answer.Split(' ').OrderBy(_ => Guid.NewGuid()).ToList();
                    }

                    return new
                    {
                        id = q.Id,
                        prompt = q.Prompt,
                        tokens,
                        level = q.Level
                    };
                }).ToList();

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    questions = clientQuestions
                };

                serverTruthJson = JsonSerializer.Serialize(questions.Select(q => new { id = q.Id, answer = q.Answer }));
                break;
            }

            case "typing_race":
            {
                var words = await _dbContext.GameWordPools
                    .OrderBy(w => EF.Functions.Random())
                    .Take(10)
                    .ToListAsync(ct);

                var clientWords = words.Select(w => new
                {
                    id = w.Id,
                    targetText = w.TargetText,
                    phonetic = w.Phonetic,
                    translation = w.Translation,
                    cefrLevel = w.CefrLevel
                }).ToList();

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    words = clientWords
                };

                serverTruthJson = JsonSerializer.Serialize(words.Select(w => new { id = w.Id, targetText = w.TargetText }));
                break;
            }

            case "fill_blitz":
            {
                var query = _dbContext.GameQuestions.Where(q => q.GameType == "fill_blitz");
                if (!string.IsNullOrWhiteSpace(difficulty) && !difficulty.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Level.ToLower() == difficulty.ToLower());
                }

                var questions = await query
                    .OrderBy(q => EF.Functions.Random())
                    .Take(12)
                    .ToListAsync(ct);

                if (questions.Count == 0)
                {
                    questions = await _dbContext.GameQuestions
                        .Where(q => q.GameType == "fill_blitz")
                        .Take(12)
                        .ToListAsync(ct);
                }

                var clientQuestions = questions.Select(q =>
                {
                    List<string> choices;
                    try
                    {
                        choices = !string.IsNullOrWhiteSpace(q.ChoicesJson)
                            ? JsonSerializer.Deserialize<List<string>>(q.ChoicesJson) ?? new List<string>()
                            : new List<string> { q.Answer };
                    }
                    catch
                    {
                        choices = new List<string> { q.Answer };
                    }

                    return new
                    {
                        id = q.Id,
                        prompt = q.Prompt,
                        choices,
                        level = q.Level
                    };
                }).ToList();

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    questions = clientQuestions
                };

                serverTruthJson = JsonSerializer.Serialize(questions.Select(q => new { id = q.Id, answer = q.Answer }));
                break;
            }

            case "sound_reflex":
            {
                var pool = await _dbContext.GameWordPools
                    .OrderBy(w => EF.Functions.Random())
                    .Take(32)
                    .ToListAsync(ct);

                var targets = pool.Take(8).ToList();
                var distractors = pool.Skip(8).ToList();

                var clientQuestions = new List<object>();
                var truths = new List<object>();

                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    var questionDistractors = distractors
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(3)
                        .ToList();

                    var allOptions = new List<GameWordPool> { target };
                    allOptions.AddRange(questionDistractors);
                    var shuffledOptions = allOptions.OrderBy(_ => Guid.NewGuid()).Select(opt => new
                    {
                        id = opt.Id,
                        targetText = opt.TargetText,
                        translation = opt.Translation
                    }).ToList();

                    clientQuestions.Add(new
                    {
                        id = target.Id,
                        audioText = target.TargetText,
                        choices = shuffledOptions
                    });

                    truths.Add(new { id = target.Id, answer = target.TargetText });
                }

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    questions = clientQuestions
                };

                serverTruthJson = JsonSerializer.Serialize(truths);
                break;
            }

            case "lingo_wordle":
            {
                var wordleQuestion = await _dbContext.GameQuestions
                    .Where(q => q.GameType == "lingo_wordle")
                    .OrderBy(q => EF.Functions.Random())
                    .FirstOrDefaultAsync(ct);

                if (wordleQuestion == null)
                {
                    throw new KeyNotFoundException("Chưa có từ vựng cho trò chơi Wordle.");
                }

                string category = "Chung";
                string meaningVi = "";
                if (!string.IsNullOrWhiteSpace(wordleQuestion.ChoicesJson))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(wordleQuestion.ChoicesJson);
                        if (doc.RootElement.TryGetProperty("category", out var catProp)) category = catProp.GetString() ?? category;
                        if (doc.RootElement.TryGetProperty("meaningVi", out var meanProp)) meaningVi = meanProp.GetString() ?? meaningVi;
                    }
                    catch { }
                }

                responsePayload = new
                {
                    attemptId,
                    gameType,
                    id = wordleQuestion.Id,
                    prompt = wordleQuestion.Prompt,
                    category,
                    meaningVi = !string.IsNullOrWhiteSpace(meaningVi) ? meaningVi : wordleQuestion.SourceRef,
                    wordLength = wordleQuestion.Answer.Trim().Length,
                    maxAttempts = 6
                };

                serverTruthJson = JsonSerializer.Serialize(new
                {
                    id = wordleQuestion.Id,
                    secretWord = wordleQuestion.Answer.Trim().ToUpperInvariant()
                });
                break;
            }

            default:
                throw new ArgumentException("Game type không hợp lệ.");
        }

        // Save active attempt for server verification
        var activeAttempt = new ActiveGameAttempt
        {
            AttemptId = attemptId,
            UserId = userId,
            GameType = gameType,
            StartedAt = DateTime.UtcNow,
            QuestionsPayloadJson = serverTruthJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ActiveGameAttempts.Add(activeAttempt);
        await _dbContext.SaveChangesAsync(ct);

        return new GameDataResponseDto(attemptId, gameType, responsePayload);
    }

    public async Task<GameSubmitResultDto> SubmitGameAsync(
        GameSubmitRequestDto request,
        Guid userId,
        string? displayName = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AttemptId))
        {
            throw new ArgumentException("AttemptId không được để trống.");
        }

        var attempt = await _dbContext.ActiveGameAttempts
            .FirstOrDefaultAsync(a => a.AttemptId == request.AttemptId, ct);

        if (attempt == null)
        {
            throw new InvalidOperationException("Phiên chơi không hợp lệ hoặc đã nộp điểm trước đó.");
        }

        // Anti-cheat physical timing check: minimum 2 seconds
        var durationSeconds = (int)Math.Max(1, (DateTime.UtcNow - attempt.StartedAt).TotalSeconds);
        if (durationSeconds < 2)
        {
            throw new InvalidOperationException("Thời gian làm bài quá nhanh (nghi vấn gian lận).");
        }

        int correctCount = 0;
        int totalQuestions = 0;
        int mistakes = 0;
        int maxConsecutive = 0;
        int curConsecutive = 0;

        var gameType = attempt.GameType.ToLowerInvariant();

        switch (gameType)
        {
            case "word_match":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var pairs = doc.RootElement.EnumerateArray().ToList();
                totalQuestions = pairs.Count;

                var groundTruth = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in pairs)
                {
                    groundTruth[p.GetProperty("id").GetString() ?? ""] = p.GetProperty("translation").GetString() ?? "";
                }

                foreach (var ans in request.Answers)
                {
                    if (groundTruth.TryGetValue(ans.QuestionId, out var expected) &&
                        string.Equals(expected.Trim(), ans.Answer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        correctCount++;
                        curConsecutive++;
                        if (curConsecutive > maxConsecutive) maxConsecutive = curConsecutive;
                    }
                    else
                    {
                        mistakes++;
                        curConsecutive = 0;
                    }
                }
                break;
            }

            case "sentence_scramble":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var questions = doc.RootElement.EnumerateArray().ToList();
                totalQuestions = questions.Count;

                var groundTruth = questions.ToDictionary(
                    q => q.GetProperty("id").GetString() ?? "",
                    q => q.GetProperty("answer").GetString() ?? "",
                    StringComparer.OrdinalIgnoreCase);

                foreach (var ans in request.Answers)
                {
                    if (groundTruth.TryGetValue(ans.QuestionId, out var expected))
                    {
                        var normExpected = string.Join(" ", expected.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
                        var normUser = string.Join(" ", ans.Answer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

                        if (normExpected == normUser)
                        {
                            correctCount++;
                            curConsecutive++;
                            if (curConsecutive > maxConsecutive) maxConsecutive = curConsecutive;
                        }
                        else
                        {
                            mistakes++;
                            curConsecutive = 0;
                        }
                    }
                    else
                    {
                        mistakes++;
                        curConsecutive = 0;
                    }
                }
                break;
            }

            case "typing_race":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var words = doc.RootElement.EnumerateArray().ToList();
                totalQuestions = words.Count;

                var groundTruth = words.ToDictionary(
                    w => w.GetProperty("id").GetString() ?? "",
                    w => w.GetProperty("targetText").GetString() ?? "",
                    StringComparer.OrdinalIgnoreCase);

                foreach (var ans in request.Answers)
                {
                    if (groundTruth.TryGetValue(ans.QuestionId, out var expected) &&
                        string.Equals(expected.Trim(), ans.Answer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        correctCount++;
                        curConsecutive++;
                        if (curConsecutive > maxConsecutive) maxConsecutive = curConsecutive;
                    }
                    else
                    {
                        mistakes++;
                        curConsecutive = 0;
                    }
                }
                break;
            }

            case "fill_blitz":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var questions = doc.RootElement.EnumerateArray().ToList();
                totalQuestions = questions.Count;

                var groundTruth = questions.ToDictionary(
                    q => q.GetProperty("id").GetString() ?? "",
                    q => q.GetProperty("answer").GetString() ?? "",
                    StringComparer.OrdinalIgnoreCase);

                foreach (var ans in request.Answers)
                {
                    if (groundTruth.TryGetValue(ans.QuestionId, out var expected) &&
                        string.Equals(expected.Trim(), ans.Answer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        correctCount++;
                        curConsecutive++;
                        if (curConsecutive > maxConsecutive) maxConsecutive = curConsecutive;
                    }
                    else
                    {
                        mistakes++;
                        curConsecutive = 0;
                    }
                }
                break;
            }

            case "sound_reflex":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var questions = doc.RootElement.EnumerateArray().ToList();
                totalQuestions = questions.Count;

                var groundTruth = questions.ToDictionary(
                    q => q.GetProperty("id").GetString() ?? "",
                    q => q.GetProperty("answer").GetString() ?? "",
                    StringComparer.OrdinalIgnoreCase);

                foreach (var ans in request.Answers)
                {
                    if (groundTruth.TryGetValue(ans.QuestionId, out var expected) &&
                        string.Equals(expected.Trim(), ans.Answer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        correctCount++;
                        curConsecutive++;
                        if (curConsecutive > maxConsecutive) maxConsecutive = curConsecutive;
                    }
                    else
                    {
                        mistakes++;
                        curConsecutive = 0;
                    }
                }
                break;
            }

            case "lingo_wordle":
            {
                using var doc = JsonDocument.Parse(attempt.QuestionsPayloadJson);
                var secretWord = doc.RootElement.GetProperty("secretWord").GetString() ?? "";
                totalQuestions = 1;

                bool won = request.Answers.Any(a => string.Equals(a.Answer.Trim(), secretWord, StringComparison.OrdinalIgnoreCase));
                if (won)
                {
                    correctCount = 1;
                    mistakes = 0;
                    maxConsecutive = 1;
                }
                else
                {
                    correctCount = 0;
                    mistakes = 1;
                    maxConsecutive = 0;
                }
                break;
            }

            default:
                throw new ArgumentException("Không xác định được loại trò chơi.");
        }

        // Calculate score
        bool hasTimer = gameType != "lingo_wordle";
        var scoreInput = new GameScoreInput(
            CorrectCount: correctCount,
            TotalQuestions: totalQuestions,
            TimeRemainingSeconds: request.TimeRemainingSeconds,
            ConsecutiveCorrect: maxConsecutive,
            Mistakes: mistakes,
            HasTimer: hasTimer);

        var scoreOutput = GameScoreCalculator.Calculate(scoreInput);

        // Wordle rule: +50 ONLY when won
        int finalScore = scoreOutput.FinalScore;
        int xpEarned = scoreOutput.XpEarned;
        if (gameType == "lingo_wordle")
        {
            if (correctCount > 0)
            {
                finalScore = 50;
                xpEarned = Math.Max(5, (int)Math.Round(50 / 4.0));
            }
            else
            {
                finalScore = 0;
                xpEarned = 0;
            }
        }

        // Save session
        var session = new GameSession(
            attempt.AttemptId,
            userId,
            gameType,
            finalScore,
            scoreOutput.Accuracy,
            xpEarned,
            durationSeconds,
            Math.Max(1, maxConsecutive),
            JsonSerializer.Serialize(new
            {
                correctCount,
                totalQuestions,
                mistakes,
                timeRemaining = request.TimeRemainingSeconds
            }));

        _dbContext.GameSessions.Add(session);

        // Update UserGameStat (CRITICAL: NoTracking global behavior requires Update() on existing entity)
        var stat = await _dbContext.UserGameStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GameType == gameType, ct);

        bool isNewHighScore = false;
        if (stat == null)
        {
            stat = new UserGameStat(userId, gameType, displayName);
            _dbContext.UserGameStats.Add(stat);
            isNewHighScore = finalScore > 0;
        }
        else
        {
            if (finalScore > stat.HighScore)
            {
                isNewHighScore = true;
            }
            // Explicitly call Update on detached entity (Persistence/Extension.cs:36 global NoTracking rule)
            _dbContext.UserGameStats.Update(stat);
        }

        stat.RecordGame(finalScore, scoreOutput.Accuracy, xpEarned, displayName);

        // Remove used attempt
        _dbContext.ActiveGameAttempts.Remove(attempt);

        await _dbContext.SaveChangesAsync(ct);

        // Award XP via GamificationService
        if (xpEarned > 0)
        {
            try
            {
                await _gamificationService.AwardXpAsync(new AwardXpRequest(
                    UserId: userId,
                    SourceType: $"game_{gameType}",
                    XpDelta: xpEarned,
                    SourceId: session.Id,
                    IdempotencyKey: session.AttemptId), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể cộng XP tự động cho user {UserId}", userId);
            }
        }

        // Stamps
        var stamps = new List<string>();
        if (Math.Abs(scoreOutput.Accuracy - 1.0) < 0.001 && totalQuestions > 0)
        {
            stamps.Add("PERFECT_SCORE");
        }

        try
        {
            var streak = await _gamificationService.GetUserStreakAsync(userId, ct);
            if (streak != null && streak.CurrentDays == 7)
            {
                stamps.Add("STREAK_7_DAYS");
            }
        }
        catch { }

        return new GameSubmitResultDto(
            Success: true,
            Score: finalScore,
            XpEarned: xpEarned,
            Accuracy: scoreOutput.Accuracy,
            ComboMultiplier: scoreOutput.ComboMultiplier,
            TimeBonus: scoreOutput.TimeBonus,
            StreakBonus: scoreOutput.StreakBonus,
            DurationSeconds: durationSeconds,
            CorrectCount: correctCount,
            TotalQuestions: totalQuestions,
            HighScore: stat.HighScore,
            IsNewHighScore: isNewHighScore,
            StampsAwarded: stamps
        );
    }

    public async Task<GameLeaderboardResponseDto> GetLeaderboardAsync(
        string? type,
        int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = _dbContext.UserGameStats.AsQueryable();
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.GameType == type.ToLower());
        }

        var topStats = await query
            .OrderByDescending(s => s.HighScore)
            .ThenByDescending(s => s.TotalXpEarned)
            .Take(limit)
            .ToListAsync(ct);

        if (topStats.Count == 0)
        {
            return new GameLeaderboardResponseDto(true, new List<GameLeaderboardEntryDto>());
        }

        var entries = topStats.Select((s, index) =>
        {
            string name = !string.IsNullOrWhiteSpace(s.DisplayName)
                ? s.DisplayName
                : $"Học viên #{s.UserId.ToString()[..Math.Min(4, s.UserId.ToString().Length)]}";

            return new GameLeaderboardEntryDto(
                Rank: index + 1,
                UserId: s.UserId.ToString(),
                DisplayName: name,
                AvatarUrl: s.AvatarUrl,
                GameType: s.GameType,
                HighScore: s.HighScore,
                TotalWins: s.TotalWins,
                TotalXpEarned: s.TotalXpEarned,
                LastPlayedAt: s.LastPlayedAt
            );
        }).ToList();

        return new GameLeaderboardResponseDto(true, entries);
    }

    public async Task<UserGameStatsResponseDto> GetUserStatsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var stats = await _dbContext.UserGameStats
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        var byGame = stats.ToDictionary(
            s => s.GameType,
            s => new GameStatDetailDto(
                HighScore: s.HighScore,
                TotalGames: s.TotalGamesPlayed,
                TotalWins: s.TotalWins,
                TotalXp: s.TotalXpEarned,
                AvgAccuracy: s.AvgAccuracy,
                LastPlayedAt: s.LastPlayedAt
            ));

        return new UserGameStatsResponseDto(
            Success: true,
            UserId: userId.ToString(),
            TotalGamesPlayed: stats.Sum(s => s.TotalGamesPlayed),
            TotalWins: stats.Sum(s => s.TotalWins),
            TotalXpEarned: stats.Sum(s => s.TotalXpEarned),
            ByGame: byGame
        );
    }
}
