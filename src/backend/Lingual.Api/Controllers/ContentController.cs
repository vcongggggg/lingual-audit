using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContentController : ControllerBase
{
    private readonly MezonBotDbContext _botDbContext;
    private readonly LearningDbContext _learningDbContext;
    private readonly GamificationDbContext _gamificationDbContext;
    private readonly IUserStreakService _streakService;
    private readonly IXpService _xpService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<ContentController> _logger;

    // Cache fallback questions for server-side evaluation when questions originate from the fallback bank
    private static readonly ConcurrentDictionary<string, FallbackQuestionMeta> FallbackBank = new();

    static ContentController()
    {
        InitializeFallbackBank();
    }

    public ContentController(
        MezonBotDbContext botDbContext,
        LearningDbContext learningDbContext,
        GamificationDbContext gamificationDbContext,
        IUserStreakService streakService,
        IXpService xpService,
        ILeaderboardService leaderboardService,
        ILogger<ContentController> logger)
    {
        _botDbContext = botDbContext;
        _learningDbContext = learningDbContext;
        _gamificationDbContext = gamificationDbContext;
        _streakService = streakService;
        _xpService = xpService;
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/content/quizzes?mode=sprint10&limit=10&questionType=multiple_choice&difficulty=1
    /// BẢO MẬT: KHÔNG trả correct_answer hay is_correct cho client để chống gian lận F12.
    /// </summary>
    [HttpGet("quizzes")]
    public async Task<IActionResult> GetQuizzes(
        [FromQuery] string mode = "sprint10",
        [FromQuery] int? limit = null,
        [FromQuery] string? questionType = null,
        [FromQuery] int? difficulty = null,
        CancellationToken cancellationToken = default)
    {
        var targetCount = limit ?? mode switch
        {
            "sprint10" => 10,
            "clan20" => 20,
            "marathon" => 300,
            _ => 10
        };

        targetCount = Math.Clamp(targetCount, 1, 300);

        try
        {
            if (_botDbContext.Database.IsRelational())
            {
                var query = _botDbContext.QuizQuestions
                    .Where(q => q.Status == "published")
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(questionType))
                {
                    query = query.Where(q => q.QuestionType == questionType);
                }

                if (difficulty.HasValue && difficulty.Value > 0)
                {
                    query = query.Where(q => q.Difficulty == (short)difficulty.Value);
                }

                // Tối ưu Index: Dùng Index-Only Scan lấy danh sách ID rồi xáo trộn ngẫu nhiên trên bộ nhớ,
                // tránh dùng ORDER BY Random() quét toàn bảng làm phá vỡ B-Tree Index trên PostgreSQL
                var allIds = await query.Select(q => q.Id).ToListAsync(cancellationToken);

                if (allIds.Count > 0)
                {
                    var selectedIds = allIds.OrderBy(_ => Guid.NewGuid()).Take(targetCount).ToList();
                    var dbQuestions = await _botDbContext.QuizQuestions
                        .Where(q => selectedIds.Contains(q.Id))
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                    var mapped = dbQuestions.Select(q =>
                    {
                        var parsedOptions = ParseQuizOptions(q.Options);
                        object? payloadObj = null;
                        if (!string.IsNullOrWhiteSpace(q.Payload) && q.Payload != "{}")
                        {
                            try { payloadObj = JsonSerializer.Deserialize<object>(q.Payload); } catch { }
                        }

                        // KHÔNG TRẢ correct_answer, KHÔNG TRẢ is_correct
                        return new
                        {
                            id = q.Id.ToString(),
                            level = q.Level ?? "A1",
                            difficulty = (int)(q.Difficulty ?? 1),
                            question_type = q.QuestionType,
                            prompt = q.Prompt,
                            prompt_audio_url = q.PromptAudioUrl,
                            options = parsedOptions.Select(o => new
                            {
                                key = o.Key,
                                text = o.Text,
                                image_url = o.ImageUrl
                            }),
                            points = (int)q.Points,
                            payload = payloadObj
                        };
                    }).ToList();

                    return Ok(new
                    {
                        mode,
                        total = mapped.Count,
                        questions = mapped
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [ContentApi] Không thể đọc từ DB quiz_questions, chuyển sang nạp từ ngân hàng dự phòng.");
        }

        // Fallback: ngân hàng câu hỏi an toàn (cũng không trả correct_answer)
        var fallbackQuestions = GetSanitizedFallbackQuestions(targetCount);
        return Ok(new
        {
            mode,
            total = fallbackQuestions.Count,
            questions = fallbackQuestions
        });
    }

    /// <summary>
    /// POST /api/v1/content/check-answer
    /// Server tự kiểm tra và trả kết quả đúng/sai kèm đáp án chuẩn và giải thích sau khi người dùng chọn.
    /// </summary>
    [HttpPost("check-answer")]
    public async Task<IActionResult> CheckSingleAnswer(
        [FromBody] CheckAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.QuestionId))
        {
            return BadRequest(new { error = "Thiếu questionId." });
        }

        var (correctAnswer, explanation) = await ResolveCorrectAnswerAsync(request.QuestionId, cancellationToken);
        var isCorrect = !string.IsNullOrWhiteSpace(request.Answer) &&
                        !string.IsNullOrWhiteSpace(correctAnswer) &&
                        request.Answer.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            questionId = request.QuestionId,
            isCorrect,
            correctAnswer,
            explanation
        });
    }

    /// <summary>
    /// POST /api/v1/content/quiz-attempts
    /// BẢO MẬT:
    /// 1. Bắt buộc đăng nhập [Authorize].
    /// 2. Lấy UserId từ Token, KHÔNG tin UserId client gửi, KHÔNG fallback lấy user đầu tiên.
    /// 3. Server tự chấm điểm từ DB, BỎ QUA cờ IsCorrect client gửi.
    /// 4. Làm sai hết thì 0 XP.
    /// 5. Idempotency key gắn với UserId + bài thi, chống cộng trùng.
    /// 6. Ghi DB lỗi thì trả 500 InternalServerError.
    /// </summary>
    [Authorize]
    [HttpPost("quiz-attempts")]
    public async Task<IActionResult> SaveQuizAttempt(
        [FromBody] QuizAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Dữ liệu kết quả bài làm không hợp lệ." });
        }

        // 1. Xác thực UserId từ JWT Token Claims
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Yêu cầu đăng nhập hợp lệ để nộp bài và nhận điểm XP." });
        }

        var answers = request.Answers ?? new List<QuizSubmittedAnswer>();
        var totalCount = answers.Count;

        // 2. Server tự chấm đáp án - Hoàn toàn không tin client
        int serverCorrectCount = 0;
        foreach (var ans in answers)
        {
            if (string.IsNullOrWhiteSpace(ans.QuestionId)) continue;
            var (correctAnswer, _) = await ResolveCorrectAnswerAsync(ans.QuestionId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(correctAnswer) &&
                !string.IsNullOrWhiteSpace(ans.Answer) &&
                ans.Answer.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                serverCorrectCount++;
            }
        }

        // 3. Quy tắc XP: Sai hết = 0 XP. Đúng câu nào nhận 10 XP câu đó.
        int xpAwarded = serverCorrectCount > 0 ? (serverCorrectCount * 10) : 0;

        // 4. Idempotency Key gắn chặt với UserId của phiên đăng nhập
        var baseKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Guid.NewGuid().ToString("N") : request.IdempotencyKey;
        var scopedIdempotencyKey = $"quiz_attempt_{userId}_{baseKey}";

        try
        {
            if (xpAwarded > 0)
            {
                var metadata = JsonSerializer.Serialize(new
                {
                    type = "web_quiz_attempt",
                    mode = request.Mode ?? "sprint10",
                    score = serverCorrectCount,
                    total = totalCount,
                    awarded_at = DateTime.UtcNow.ToString("o")
                });

                var awardResult = await _xpService.AwardXpAsync(new AwardXpRequest(
                    UserId: userId,
                    SourceType: "quiz_attempt",
                    XpDelta: xpAwarded,
                    SourceId: Guid.NewGuid(),
                    IdempotencyKey: scopedIdempotencyKey,
                    Metadata: metadata
                ), cancellationToken);

                if (awardResult.AlreadyAwarded)
                {
                    return Ok(new
                    {
                        success = true,
                        xpAwarded = 0,
                        xp = awardResult.TotalXp,
                        isDuplicate = true,
                        correctCount = serverCorrectCount,
                        totalQuestions = totalCount,
                        message = "Bài làm này đã được ghi nhận trước đó. Không cộng thêm XP trùng lặp."
                    });
                }

                _logger.LogInformation("🎯 [ContentApi] Đã lưu Quiz Attempt ({Correct}/{Total}) nhận +{Xp} XP cho User {UserId}",
                    serverCorrectCount, totalCount, xpAwarded, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ContentApi] Lỗi cơ sở dữ liệu khi ghi nhận Quiz Attempt");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Lỗi cơ sở dữ liệu khi ghi nhận kết quả bài thi.",
                success = false
            });
        }

        return Ok(new
        {
            success = true,
            xpAwarded = xpAwarded,
            xp = xpAwarded,
            correctCount = serverCorrectCount,
            totalQuestions = totalCount,
            message = serverCorrectCount > 0
                ? $"Tuyệt vời! Bạn trả lời đúng {serverCorrectCount}/{totalCount} câu và nhận được +{xpAwarded} XP!"
                : $"Bạn chưa trả lời đúng câu nào (0/{totalCount}). Hãy ôn tập thêm và thử lại nhé!"
        });
    }

    /// <summary>
    /// POST /api/v1/content/reviews
    /// Lưu thật tiến độ ôn tập thẻ từ vựng SRS vào bảng srs_cards trong PostgreSQL
    /// </summary>
    [Authorize]
    [HttpPost("reviews")]
    public async Task<IActionResult> SaveReview(
        [FromBody] SrsReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.VocabularyId))
        {
            return BadRequest(new { error = "Dữ liệu ôn tập không hợp lệ (thiếu vocabularyId)." });
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Yêu cầu đăng nhập hợp lệ để lưu tiến độ ôn tập SRS." });
        }

        if (!Guid.TryParse(request.VocabularyId, out var vocabId))
        {
            return BadRequest(new { error = "Mã từ vựng vocabularyId không đúng định dạng GUID." });
        }

        try
        {
            if (_learningDbContext.Database.IsRelational())
            {
                var conn = _learningDbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                var reviewItem = JsonSerializer.Serialize(new
                {
                    rating = request.Rating,
                    quality = request.Quality,
                    time_spent_ms = request.TimeSpentMs,
                    reviewed_at = DateTime.UtcNow.ToString("o")
                });

                var nextDue = request.NextDueAt ?? DateTime.UtcNow.AddDays(Math.Max(1, (double)request.IntervalDays));

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO srs_cards (user_id, vocabulary_id, repetitions, interval_days, ease_factor, next_due_at, last_reviewed_at, review_history, created_at, updated_at)
                    VALUES (@userId, @vocabId, @repetitions, @intervalDays, @easeFactor, @nextDue, now(), jsonb_build_array(@reviewItem::jsonb), now(), now())
                    ON CONFLICT (user_id, vocabulary_id) DO UPDATE SET
                        repetitions = EXCLUDED.repetitions,
                        interval_days = EXCLUDED.interval_days,
                        ease_factor = EXCLUDED.ease_factor,
                        next_due_at = EXCLUDED.next_due_at,
                        last_reviewed_at = now(),
                        review_history = srs_cards.review_history || jsonb_build_array(@reviewItem::jsonb),
                        updated_at = now();";

                AddParam(cmd, "@userId", userId);
                AddParam(cmd, "@vocabId", vocabId);
                AddParam(cmd, "@repetitions", request.Repetitions);
                AddParam(cmd, "@intervalDays", (decimal)request.IntervalDays);
                AddParam(cmd, "@easeFactor", (decimal)request.EaseFactor);
                AddParam(cmd, "@nextDue", nextDue);
                AddParam(cmd, "@reviewItem", reviewItem);

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation("📚 [ContentApi] Đã lưu tiến độ SRS cho User {UserId}, Từ vựng {VocabId} (Lần lặp {Reps})",
                    userId, vocabId, request.Repetitions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ContentApi] Lỗi khi lưu tiến độ SRS vào cơ sở dữ liệu");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Lỗi cơ sở dữ liệu khi lưu tiến độ ôn tập từ vựng.",
                success = false
            });
        }

        return Ok(new
        {
            success = true,
            vocabularyId = vocabId,
            repetitions = request.Repetitions,
            message = "Đã lưu tiến độ ôn tập thẻ từ vựng vào cơ sở dữ liệu."
        });
    }

    /// <summary>
    /// GET /api/v1/content/leaderboard
    /// Sử dụng LeaderboardService của Gamification module (Redis / SQL fallback), không dùng raw SQL và không có user mẫu giả.
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _leaderboardService.GetLeaderboardAsync(limit: 50, cancellationToken: cancellationToken);
            var entries = response.Entries.Select(e => new
            {
                rank = e.Rank,
                user_id = e.UserId,
                name = e.Name,
                xp = (int)e.Xp,
                level = e.Xp > 500 ? "B1" : e.Xp > 200 ? "A2" : "A1",
                words_learned = Math.Max(10, (int)e.Xp / 10),
                streak = 1
            }).ToList();

            return Ok(new { data = entries });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ContentApi] Lỗi khi tra cứu leaderboard từ LeaderboardService.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Lỗi khi lấy thông tin bảng xếp hạng.",
                data = new List<object>()
            });
        }
    }

    /// <summary>
    /// GET /api/v1/content/vocabulary?limit=50
    /// </summary>
    [HttpGet("vocabulary")]
    public async Task<IActionResult> GetVocabulary(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        try
        {
            if (_learningDbContext.Database.IsRelational())
            {
                var items = await _learningDbContext.VocabularyItems
                    .Where(v => v.Status == "published")
                    .AsNoTracking()
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                if (items.Count > 0)
                {
                    return Ok(new
                    {
                        total = items.Count,
                        data = items.Select(v => new
                        {
                            id = v.Id.ToString(),
                            term = v.Term,
                            ipa = v.Ipa ?? string.Empty,
                            meaning = v.Meaning,
                            part_of_speech = v.PartOfSpeech ?? "noun",
                            image_url = v.ImageUrl,
                            audio_url = v.PronunciationAudioUrl
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [ContentApi] Lỗi khi lấy danh sách từ vựng từ DB");
        }

        var defaultVocab = new List<object>
        {
            new { id = "vocab-1", term = "breakfast", ipa = "/ˈbrek.fəst/", meaning = "Bữa sáng", part_of_speech = "noun" },
            new { id = "vocab-2", term = "airport", ipa = "/ˈer.pɔːt/", meaning = "Sân bay", part_of_speech = "noun" },
            new { id = "vocab-3", term = "coffee", ipa = "/ˈkɒf.i/", meaning = "Cà phê", part_of_speech = "noun" }
        };

        return Ok(new
        {
            total = defaultVocab.Count,
            data = defaultVocab
        });
    }

    // ==========================================
    // HELPERS & RESOLVERS
    // ==========================================

    private async Task<(string CorrectAnswer, string Explanation)> ResolveCorrectAnswerAsync(string questionId, CancellationToken ct)
    {
        // 1. Thử tìm trong DB
        if (Guid.TryParse(questionId, out var qGuid))
        {
            try
            {
                var dbQ = await _botDbContext.QuizQuestions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == qGuid, ct);

                if (dbQ != null)
                {
                    var options = ParseQuizOptions(dbQ.Options);
                    var correct = options.FirstOrDefault(o => o.IsCorrect)?.Key ?? "A";
                    return (correct, dbQ.Explanation ?? string.Empty);
                }
            }
            catch { }
        }

        // 2. Thử tìm trong Fallback Bank
        if (FallbackBank.TryGetValue(questionId, out var fallback))
        {
            return (fallback.CorrectAnswer, fallback.Explanation);
        }

        // 3. Không tìm thấy câu hỏi trong DB và không có trong FallbackBank -> trả chuỗi rỗng để không bao giờ chấm đúng ngẫu nhiên
        return (string.Empty, string.Empty);
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static List<OptionDto> ParseQuizOptions(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var options = JsonSerializer.Deserialize<List<OptionDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return options ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static List<object> GetSanitizedFallbackQuestions(int count)
    {
        return FallbackBank.Values.Take(count).Select(f => (object)new
        {
            id = f.Id,
            level = f.Level,
            difficulty = f.Difficulty,
            question_type = f.QuestionType,
            prompt = f.Prompt,
            prompt_audio_url = (string?)null,
            options = f.Options.Select(o => new { key = o.Key, text = o.Text, image_url = (string?)null }),
            points = 10,
            payload = (object?)null
            // KHÔNG TRẢ correct_answer, KHÔNG TRẢ explanation
        }).ToList();
    }

    private static void InitializeFallbackBank()
    {
        var q1 = new FallbackQuestionMeta(
            "fb-01", "A1", 1, "multiple_choice",
            "Choose the word that completes the sentence: I eat ___ every morning.",
            new[] { new OptionDto { Key = "A", Text = "breakfast", IsCorrect = true }, new OptionDto { Key = "B", Text = "midnight" }, new OptionDto { Key = "C", Text = "yesterday" }, new OptionDto { Key = "D", Text = "quickly" } },
            "A", "Breakfast nghĩa là bữa sáng, phù hợp hoàn cảnh every morning."
        );
        var q2 = new FallbackQuestionMeta(
            "fb-02", "A1", 1, "multiple_choice",
            "Welcome to London! May I see your ___ and flight ticket?",
            new[] { new OptionDto { Key = "A", Text = "passport", IsCorrect = true }, new OptionDto { Key = "B", Text = "pillow" }, new OptionDto { Key = "C", Text = "kitchen" }, new OptionDto { Key = "D", Text = "window" } },
            "A", "Passport là hộ chiếu, giấy tờ cần xuất trình cùng vé máy bay."
        );
        var q3 = new FallbackQuestionMeta(
            "fb-03", "A2", 1, "multiple_choice",
            "What size coffee would you like? We have small, medium, and ___.",
            new[] { new OptionDto { Key = "A", Text = "large", IsCorrect = true }, new OptionDto { Key = "B", Text = "heavy" }, new OptionDto { Key = "C", Text = "slow" }, new OptionDto { Key = "D", Text = "short" } },
            "A", "Large là kích cỡ lớn trong các lựa chọn đồ uống quán cà phê."
        );

        FallbackBank[q1.Id] = q1;
        FallbackBank[q2.Id] = q2;
        FallbackBank[q3.Id] = q3;
    }

    public record OptionDto
    {
        public string Key { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public bool IsCorrect { get; init; }
        public string? ImageUrl { get; init; }
    }

    private record FallbackQuestionMeta(
        string Id,
        string Level,
        int Difficulty,
        string QuestionType,
        string Prompt,
        OptionDto[] Options,
        string CorrectAnswer,
        string Explanation
    );
}

public record CheckAnswerRequest(
    string QuestionId,
    string Answer
);

public record QuizAttemptRequest(
    string? IdempotencyKey,
    string? UserId,
    string? Mode,
    List<QuizSubmittedAnswer>? Answers
);

public record QuizSubmittedAnswer(
    string? QuestionId,
    string? Answer,
    bool? IsCorrect = null
);

public record SrsReviewRequest(
    string VocabularyId,
    string? Rating,
    int Quality,
    int Repetitions,
    decimal IntervalDays,
    decimal EaseFactor,
    DateTime? NextDueAt,
    int TimeSpentMs,
    string? IdempotencyKey
);
