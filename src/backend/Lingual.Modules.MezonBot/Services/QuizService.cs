using System.Text.Json;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.MezonBot.Contracts;
using Lingual.Modules.MezonBot.Contracts.DTOs;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class QuizService(
    MezonBotDbContext dbContext,
    IGamificationService gamificationService,
    ILogger<QuizService> logger,
    IAiQuotaService? aiQuotaService = null) : IQuizService
{
    private static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const int DefaultQuizXp = 20;

    public async Task<QuizListResponseDto> GetQuizQuestionsAsync(
        QuizQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(parameters.Limit, 1, 300);

        try
        {
            var query = dbContext.QuizQuestions
                .AsNoTracking()
                .Where(q => q.Status == "published" && q.QuestionType == "multiple_choice");

            if (!string.IsNullOrWhiteSpace(parameters.Level))
            {
                query = query.Where(q => q.Level == parameters.Level);
            }

            var questionsRaw = await query
                .OrderBy(q => q.SortOrder)
                .ThenBy(q => q.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var questions = questionsRaw.Select(q =>
            {
                JsonElement optionsElement;
                try
                {
                    optionsElement = JsonSerializer.Deserialize<JsonElement>(q.Options);
                }
                catch
                {
                    optionsElement = default;
                }

                string? correctKey = null;
                if (optionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var opt in optionsElement.EnumerateArray())
                    {
                        if (opt.TryGetProperty("is_correct", out var isCorrect) && isCorrect.GetBoolean() &&
                            opt.TryGetProperty("key", out var keyProp))
                        {
                            correctKey = keyProp.GetString();
                            break;
                        }
                    }
                }

                return new QuizQuestionDto(
                    Id: q.Id.ToString(),
                    SortOrder: q.SortOrder,
                    Level: q.Level ?? "A1",
                    Difficulty: q.Difficulty ?? (short)1,
                    QuestionType: q.QuestionType,
                    Prompt: q.Prompt,
                    Options: optionsElement,
                    CorrectAnswer: correctKey ?? "A",
                    Explanation: q.Explanation,
                    Points: (int)q.Points);
            }).ToList();

            return new QuizListResponseDto(questions.Count, questions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi truy vấn câu hỏi trắc nghiệm từ cơ sở dữ liệu");
            return new QuizListResponseDto(0, Array.Empty<QuizQuestionDto>());
        }
    }

    public async Task<QuizAttemptResultDto> SubmitQuizAttemptAsync(
        QuizAttemptRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var answers = request.Answers ?? new List<QuizAnswerItemDto>();
        int correctCount = answers.Count(a => a.IsCorrect);
        int totalCount = answers.Count;

        if (!userId.HasValue)
        {
            // Khách chưa đăng nhập: trả kết quả trực tiếp, không tích điểm XP hay lưu lịch sử
            return new QuizAttemptResultDto(Saved: false, CorrectAnswers: correctCount, Xp: 0);
        }

        var effectiveUserId = userId.Value;

        // 1. Lưu phiên làm bài vào bảng quiz_attempts
        try
        {
            var practiceQuiz = await dbContext.Quizzes
                .FirstOrDefaultAsync(q => q.QuizType == "practice", cancellationToken);

            if (practiceQuiz == null)
            {
                practiceQuiz = new Quiz
                {
                    Id = Guid.NewGuid(),
                    Code = "practice_quiz_default",
                    Title = "Luyện tập Trắc nghiệm Từ vựng",
                    QuizType = "practice",
                    Status = "published"
                };
                dbContext.Quizzes.Add(practiceQuiz);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var attempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                QuizId = practiceQuiz.Id,
                UserId = effectiveUserId,
                Status = "submitted",
                Score = correctCount,
                MaxScore = totalCount,
                CorrectCount = (short)correctCount,
                AnswersDetail = JsonSerializer.Serialize(answers),
                StartedAt = DateTime.UtcNow.AddMinutes(-2),
                SubmittedAt = DateTime.UtcNow
            };
            dbContext.QuizAttempts.Add(attempt);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể lưu bản ghi quiz_attempts cho user {UserId}", effectiveUserId);
        }

        // 2. Thưởng điểm XP idempotent qua GamificationService
        int xpToAward = request.Xp > 0 ? request.Xp : DefaultQuizXp;
        var awardResult = await gamificationService.AwardXpAsync(new AwardXpRequest(
            UserId: effectiveUserId,
            SourceType: "clan_quiz",
            XpDelta: xpToAward,
            SourceId: null,
            IdempotencyKey: request.IdempotencyKey,
            Metadata: null), cancellationToken);

        int awardedXp = awardResult.AlreadyAwarded ? 0 : xpToAward;

        // 3. Thưởng +1 lượt AI dựa trên chấm điểm phía server từ bảng quiz_questions (>= 60%), tuyệt đối không tin a.IsCorrect của client
        if (aiQuotaService != null && !awardResult.AlreadyAwarded && totalCount > 0)
        {
            try
            {
                var verifiedCorrect = await CountServerVerifiedCorrectAnswersAsync(answers, cancellationToken);
                if ((double)verifiedCorrect / totalCount >= 0.60)
                {
                    var keySuffix = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                        ? Guid.NewGuid().ToString("N")
                        : request.IdempotencyKey.Trim();
                    await aiQuotaService.GrantBonusCreditsAsync(
                        userId: effectiveUserId,
                        amount: 1,
                        reason: "reward_quiz",
                        idempotencyKey: $"reward_quiz_{effectiveUserId:N}_{keySuffix}",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không thể cộng lượt AI thưởng sau QuizService cho user {UserId}", effectiveUserId);
            }
        }

        return new QuizAttemptResultDto(Saved: true, CorrectAnswers: correctCount, Xp: awardedXp);
    }

    private async Task<int> CountServerVerifiedCorrectAnswersAsync(
        List<QuizAnswerItemDto> answers,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<Guid>();
        var submittedByQuestionId = new Dictionary<Guid, string>();
        foreach (var ans in answers)
        {
            if (string.IsNullOrWhiteSpace(ans.QuestionId) || string.IsNullOrWhiteSpace(ans.SelectedKey)) continue;
            if (Guid.TryParse(ans.QuestionId.Trim(), out var qId) && seen.Add(qId))
            {
                submittedByQuestionId[qId] = ans.SelectedKey.Trim();
            }
        }

        if (submittedByQuestionId.Count == 0)
        {
            return 0;
        }

        var questionIds = submittedByQuestionId.Keys.ToList();
        var dbQuestions = await dbContext.QuizQuestions
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync(cancellationToken);

        int verifiedCorrect = 0;
        foreach (var q in dbQuestions)
        {
            if (!submittedByQuestionId.TryGetValue(q.Id, out var selectedKey)) continue;
            try
            {
                using var doc = JsonDocument.Parse(q.Options);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                foreach (var opt in doc.RootElement.EnumerateArray())
                {
                    if (opt.TryGetProperty("is_correct", out var isCorr) && isCorr.GetBoolean() &&
                        opt.TryGetProperty("key", out var keyProp) &&
                        string.Equals(keyProp.GetString()?.Trim(), selectedKey, StringComparison.OrdinalIgnoreCase))
                    {
                        verifiedCorrect++;
                        break;
                    }
                }
            }
            catch { }
        }

        return verifiedCorrect;
    }
}
