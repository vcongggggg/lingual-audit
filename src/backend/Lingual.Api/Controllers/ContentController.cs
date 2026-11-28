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
using Lingual.Api;
using Lingual.Modules.AITutor.Contracts;
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
    private readonly IAiQuotaService? _aiQuotaService;

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
        ILogger<ContentController> logger,
        IAiQuotaService? aiQuotaService = null)
    {
        _botDbContext = botDbContext;
        _learningDbContext = learningDbContext;
        _gamificationDbContext = gamificationDbContext;
        _streakService = streakService;
        _xpService = xpService;
        _leaderboardService = leaderboardService;
        _logger = logger;
        _aiQuotaService = aiQuotaService;
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

        // 2. Server tự chấm đáp án - Hoàn toàn không tin client (lọc trùng QuestionId chống gian lận)
        int serverCorrectCount = 0;
        var gradedQuestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ans in answers)
        {
            if (string.IsNullOrWhiteSpace(ans.QuestionId) || !gradedQuestions.Add(ans.QuestionId.Trim())) continue;
            var submittedValue = !string.IsNullOrWhiteSpace(ans.Answer) ? ans.Answer : ans.SelectedKey;
            var (correctAnswer, _) = await ResolveCorrectAnswerAsync(ans.QuestionId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(correctAnswer) &&
                !string.IsNullOrWhiteSpace(submittedValue) &&
                submittedValue.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
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

        // 5. Thưởng +1 lượt AI nếu tỷ lệ trả lời đúng do server chấm đạt >= 60%
        int aiCreditsEarned = 0;
        if (_aiQuotaService != null && totalCount > 0 && (double)serverCorrectCount / totalCount >= 0.60)
        {
            try
            {
                var grant = await _aiQuotaService.GrantBonusCreditsAsync(
                    userId: userId,
                    amount: 1,
                    reason: "reward_quiz",
                    idempotencyKey: $"reward_quiz_{userId:N}_{scopedIdempotencyKey}",
                    cancellationToken: cancellationToken);
                aiCreditsEarned = grant.CreditsAdded;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ContentApi] Không thể cộng lượt AI thưởng sau Quiz cho User {UserId}", userId);
            }
        }

        return Ok(new
        {
            success = true,
            xpAwarded = xpAwarded,
            xp = xpAwarded,
            correctCount = serverCorrectCount,
            totalQuestions = totalCount,
            aiCreditsEarned,
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

        int xpAwarded = 0;
        try
        {
            var reviewXp = request.Rating?.ToLowerInvariant() switch
            {
                "easy" => 15,
                "good" => 10,
                "hard" => 5,
                _ => 5
            };
            var baseReviewKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"{vocabId}_{DateTime.UtcNow:yyyyMMddHHmm}"
                : request.IdempotencyKey;
            var awardResult = await _xpService.AwardXpAsync(new AwardXpRequest(
                UserId: userId,
                SourceType: "srs_review",
                XpDelta: reviewXp,
                SourceId: vocabId,
                IdempotencyKey: $"srs_review_{userId}_{baseReviewKey}",
                Metadata: JsonSerializer.Serialize(new { rating = request.Rating, quality = request.Quality })
            ), cancellationToken);
            if (awardResult.IsSuccess && !awardResult.AlreadyAwarded)
            {
                xpAwarded = awardResult.AwardedXp;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [ContentApi] Lưu thẻ SRS thành công nhưng không thể cộng XP cho User {UserId}", userId);
        }

        // Thưởng +1 lượt AI khi mốc ôn tập >= 5 thẻ trong ngày (GMT+7), đếm trực tiếp từ bảng srs_cards trên server
        int aiCreditsEarned = 0;
        if (_aiQuotaService != null)
        {
            try
            {
                var (todayVn, todayStartUtc) = GetVietnamTodayBoundsUtc();
                int reviewedTodayCount = 0;
                if (_learningDbContext.Database.IsRelational())
                {
                    var conn = _learningDbContext.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }
                    using var countCmd = conn.CreateCommand();
                    countCmd.CommandText = "SELECT COUNT(*) FROM srs_cards WHERE user_id = @userId AND last_reviewed_at >= @todayStartUtc;";
                    AddParam(countCmd, "@userId", userId);
                    AddParam(countCmd, "@todayStartUtc", todayStartUtc);
                    var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
                    reviewedTodayCount = Convert.ToInt32(scalar ?? 0);
                }
                else
                {
                    reviewedTodayCount = await _learningDbContext.SrsCards
                        .AsNoTracking()
                        .CountAsync(c => c.UserId == userId && c.LastReviewedAt != null && c.LastReviewedAt >= todayStartUtc, cancellationToken);
                }

                if (reviewedTodayCount >= 5)
                {
                    var milestone = reviewedTodayCount / 5;
                    var grant = await _aiQuotaService.GrantBonusCreditsAsync(
                        userId: userId,
                        amount: 1,
                        reason: "reward_flashcard",
                        idempotencyKey: $"reward_flashcard_{userId:N}_{todayVn:yyyyMMdd}_m{milestone}",
                        cancellationToken: cancellationToken);
                    aiCreditsEarned = grant.CreditsAdded;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ContentApi] Không thể cộng lượt AI thưởng sau ôn Flashcard cho User {UserId}", userId);
            }
        }

        return Ok(new
        {
            success = true,
            vocabularyId = vocabId,
            repetitions = request.Repetitions,
            xpAwarded,
            aiCreditsEarned,
            message = "Đã lưu tiến độ ôn tập thẻ từ vựng vào cơ sở dữ liệu."
        });
    }

    private static (DateOnly TodayVn, DateTime TodayStartUtc) GetVietnamTodayBoundsUtc()
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException)
        {
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch (TimeZoneNotFoundException) { tz = TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT"); }
        }
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var todayVn = DateOnly.FromDateTime(vnNow);
        var vnStart = todayVn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (todayVn, TimeZoneInfo.ConvertTimeToUtc(vnStart, tz));
    }

    /// <summary>Returns the signed-in learner's studied words and cards due for review.</summary>
    [Authorize]
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviewProgress(CancellationToken cancellationToken = default)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "Sign in to view your vocabulary progress." });

        var now = DateTime.UtcNow;
        var cards = await _learningDbContext.SrsCards.AsNoTracking()
            .Where(card => card.UserId == userId && card.SuspendedAt == null)
            .Include(card => card.VocabularyItem)
            .Where(card => card.VocabularyItem != null && card.VocabularyItem.Status == "published")
            .OrderBy(card => card.NextDueAt)
            .ToListAsync(cancellationToken);

        var words = cards.Select(card => new
        {
            id = card.VocabularyItem!.Id.ToString(),
            term = card.VocabularyItem.Term,
            normalized_term = card.VocabularyItem.NormalizedTerm,
            source_language = card.VocabularyItem.SourceLanguage,
            target_language = card.VocabularyItem.TargetLanguage,
            ipa = card.VocabularyItem.Ipa,
            cefr_level = card.VocabularyItem.CefrLevel,
            meaning = card.VocabularyItem.Meaning,
            part_of_speech = card.VocabularyItem.PartOfSpeech ?? "noun",
            image_url = card.VocabularyItem.ImageUrl,
            pronunciation_audio_url = card.VocabularyItem.PronunciationAudioUrl,
            examples = ParseExamples(card.VocabularyItem.Examples),
            repetitions = card.Repetitions,
            next_due_at = card.NextDueAt
        }).ToList();

        return Ok(new
        {
            totalLearned = words.Count,
            totalDue = cards.Count(card => card.NextDueAt <= now),
            learnedWords = words,
            dueWords = words.Where((_, index) => cards[index].NextDueAt <= now).ToList()
        });
    }

    /// <summary>
    /// GET /api/v1/content/leaderboard
    /// Sử dụng LeaderboardService của Gamification module (Redis / SQL fallback), không dùng raw SQL và không có user mẫu giả.
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string type = "all-time",
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _leaderboardService.GetLeaderboardAsync(limit: Math.Clamp(limit, 1, 100), type: type, cancellationToken: cancellationToken);
            var entries = response.Entries.Select(e => new
            {
                rank = e.Rank,
                user_id = e.UserId,
                name = e.Name,
                avatar = e.AvatarUrl,
                xp = (int)e.Xp,
                level = e.Xp > 500 ? "B1" : e.Xp > 200 ? "A2" : "A1",
                words_learned = Math.Max(0, e.WordsLearned),
                streak = e.StreakDays
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
    [HttpGet("vocabulary/groups")]
    public async Task<IActionResult> GetVocabularyGroups(CancellationToken cancellationToken = default)
    {
        object[] groups = VocabularySeed.Groups;
        try
        {
            if (_learningDbContext.Database.IsRelational())
            {
                var vocabulary = await _learningDbContext.VocabularyItems.AsNoTracking()
                    .Where(v => v.Status == "published")
                    .ToListAsync(cancellationToken);
                if (vocabulary.Count > 0)
                {
                    var counts = vocabulary.GroupBy(word => word.CefrLevel).ToDictionary(group => group.Key, group => group.Count());
                    var chapterCounts = vocabulary.GroupBy(word => word.CefrLevel)
                        .ToDictionary(group => group.Key, group => group.Select(VocabularyTopics.Classify).Select(topic => topic.Id).Distinct().Count());
                    groups = VocabularySeed.Groups.Select(group => (object)new
                    {
                        group.level,
                        group.title,
                        group.description,
                        group.target_words,
                        word_count = CountOrSeed(counts, group.level, group.word_count),
                        chapters = chapterCounts.TryGetValue(group.level, out var chapterCount) ? chapterCount : 0
                    }).ToArray();
                }
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not count vocabulary groups."); }
        return Ok(new { groups });
    }

    [HttpGet("vocabulary/chapters")]
    public async Task<IActionResult> GetVocabularyChapters([FromQuery] string level, CancellationToken cancellationToken = default)
    {
        var normalizedLevel = level?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedLevel)) return BadRequest(new { error = "A CEFR level is required." });

        List<Lingual.Modules.Learning.Domain.VocabularyItem> items;
        try
        {
            items = _learningDbContext.Database.IsRelational()
                ? await _learningDbContext.VocabularyItems.AsNoTracking()
                    .Where(word => word.Status == "published" && word.CefrLevel == normalizedLevel)
                    .ToListAsync(cancellationToken)
                : VocabularySeed.Words.Where(word => word.CefrLevel == normalizedLevel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load vocabulary chapters for {Level}.", normalizedLevel);
            items = VocabularySeed.Words.Where(word => word.CefrLevel == normalizedLevel).ToList();
        }
        if (items.Count == 0) items = VocabularySeed.Words.Where(word => word.CefrLevel == normalizedLevel).ToList();

        var chapters = items.GroupBy(VocabularyTopics.Classify)
            .Select(group => new
            {
                id = group.Key.Id,
                title = group.Key.Title,
                description = group.Key.Description,
                word_count = group.Count()
            })
            .OrderBy(chapter => chapter.id == "general" ? 1 : 0)
            .ThenBy(chapter => chapter.title)
            .ToList();

        return Ok(new { level = normalizedLevel, chapters });
    }

    [HttpGet("vocabulary")]
    public async Task<IActionResult> GetVocabulary([FromQuery] int limit = 50, [FromQuery] string? level = null, [FromQuery] string? q = null, [FromQuery] string? topic = null, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 6000);
        var search = q?.Trim();
        try
        {
            if (_learningDbContext.Database.IsRelational())
            {
                var query = _learningDbContext.VocabularyItems.Where(v => v.Status == "published");
                if (!string.IsNullOrWhiteSpace(level)) query = query.Where(v => v.CefrLevel == level.ToUpperInvariant());
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var normalizedSearch = search.ToLowerInvariant();
                    query = query.Where(v => v.Term.ToLower().Contains(normalizedSearch)
                        || v.Meaning.ToLower().Contains(normalizedSearch)
                        || (v.Ipa != null && v.Ipa.ToLower().Contains(normalizedSearch))
                        || v.CefrLevel.ToLower().Contains(normalizedSearch));
                    query = query.OrderByDescending(v => v.Term.ToLower() == normalizedSearch)
                        .ThenByDescending(v => v.Term.ToLower().StartsWith(normalizedSearch))
                        .ThenBy(v => v.Term);
                }
                else if (!string.IsNullOrWhiteSpace(topic))
                {
                    query = query.OrderBy(v => v.Term);
                }
                var items = await query.AsNoTracking().Take(string.IsNullOrWhiteSpace(topic) ? limit : 6000).ToListAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(topic)) items = items.Where(word => VocabularyTopics.Classify(word).Id == topic).Take(limit).ToList();
                if (items.Count > 0)
                {
                    var words = items.Select(ToVocabularyDto).ToList();
                    return Ok(new { total = words.Count, words, data = words });
                }
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Vocabulary database unavailable; using bundled starter data."); }
        var seedWords = VocabularySeed.Words.Where(w => (string.IsNullOrWhiteSpace(level) || w.CefrLevel == level.ToUpperInvariant())
            && (string.IsNullOrWhiteSpace(search) || w.Term.Contains(search, StringComparison.OrdinalIgnoreCase)
                || w.Meaning.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (w.Ipa?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || w.CefrLevel.Contains(search, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(topic) || VocabularyTopics.Classify(w).Id == topic))
            .Take(limit).Select(ToVocabularyDto).ToList();
        return Ok(new { total = seedWords.Count, words = seedWords, data = seedWords });
    }

    private static object ToVocabularyDto(Lingual.Modules.Learning.Domain.VocabularyItem v)
    {
        var examples = ParseExamples(v.Examples);
        var topic = VocabularyTopics.Classify(v);
        return new { id = v.Id.ToString(), term = v.Term, normalized_term = v.NormalizedTerm, source_language = v.SourceLanguage, target_language = v.TargetLanguage, ipa = v.Ipa, cefr_level = v.CefrLevel, topic_id = topic.Id, topic = topic.Title, meaning = v.Meaning, part_of_speech = v.PartOfSpeech ?? "noun", image_url = v.ImageUrl, pronunciation_audio_url = v.PronunciationAudioUrl, examples };
    }

    private static object[] ParseExamples(string json)
    {
        try { return JsonSerializer.Deserialize<object[]>(json) ?? Array.Empty<object>(); }
        catch { return Array.Empty<object>(); }
    }

    private static int CountOrSeed(IReadOnlyDictionary<string, int> counts, string level, int seedCount) =>
        counts.TryGetValue(level, out var count) && count > 0 ? count : seedCount;

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
    bool? IsCorrect = null,
    string? SelectedKey = null
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
