using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Endpoints;

public class GamificationEndpoints : IEndpointBase
{
    private const string DemoUserKey = "lingual-local-demo-user";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gamification")
            .WithTags("Gamification");

        // 1. Leaderboard API
        group.MapGet("/leaderboard", async (
            [FromQuery] int limit = 20,
            [FromQuery] string type = "all-time",
            HttpContext httpContext = null!,
            [FromServices] IGamificationService gamificationService = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetCurrentUserId(httpContext);
            var result = await gamificationService.GetLeaderboardAsync(limit, type, userId, ct);
            return Results.Ok(result);
        })
        .WithName("GetGamificationLeaderboard")
        .WithSummary("Lấy bảng xếp hạng điểm XP (Redis)")
        .WithDescription("Truy vấn Top học viên theo tuần hoặc mọi thời đại từ Redis Sorted Sets.")
        .Produces<LeaderboardResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 2. Streak Info API
        group.MapGet("/streak/me", async (
            [FromQuery] Guid? userId,
            HttpContext httpContext,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            var currentUserId = userId ?? GetCurrentUserId(httpContext);
            if (!currentUserId.HasValue)
            {
                return Results.BadRequest(new { Message = "Không tìm thấy thông tin người dùng." });
            }

            var streak = await gamificationService.GetUserStreakAsync(currentUserId.Value, ct);
            return Results.Ok(streak);
        })
        .WithName("GetMyStreak")
        .WithSummary("Lấy thông tin chuỗi ngày học Streak")
        .WithDescription("Trả về số ngày Streak hiện tại, kỷ lục, trạng thái Active hôm nay và số khiên Streak Freeze.")
        .Produces<UserStreakDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 3. Award XP API (Nội bộ / Admin / Learning integration)
        group.MapPost("/xp/award", async (
            [FromBody] AwardXpRequest request,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            var result = await gamificationService.AwardXpAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("AwardXp")
        .WithSummary("Cộng điểm XP và ghi sổ cái xp_ledger")
        .WithDescription("Cộng điểm thưởng idempotent, cập nhật Redis Leaderboard và chuỗi Streak.")
        .Produces<AwardXpResult>(StatusCodes.Status200OK)
        .Produces<AwardXpResult>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // 4. Content Leaderboard Alias (Tương thích 100% Postman Sprint 1 & Web App)
        app.MapGet("/api/v1/content/leaderboard", async (
            [FromQuery] int limit = 20,
            HttpContext httpContext = null!,
            [FromServices] IGamificationService gamificationService = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetCurrentUserId(httpContext);
            var result = await gamificationService.GetLeaderboardAsync(limit, "all-time", userId, ct);
            return Results.Ok(result);
        })
        .WithName("GetContentLeaderboardAlias")
        .WithTags("Gamification")
        .WithSummary("Bảng xếp hạng (Alias tương thích Postman & WebApp)")
        .Produces<LeaderboardResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 5. Content Reviews (Idempotent XP awarding tương thích Postman Sprint 1)
        app.MapPost("/api/v1/content/reviews", async (
            [FromBody] ReviewSubmissionDto request,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.VocabularyId) ||
                !Guid.TryParse(request.VocabularyId, out var vocabGuid) ||
                request.Quality < 0 || request.Quality > 5 ||
                request.Repetitions < 0 || request.IntervalDays < 0 || request.EaseFactor < 1.3m)
            {
                return Results.BadRequest(new { error = "Invalid review payload" });
            }

            // Demo user ID giả lập khi gọi không có token
            var demoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            const int reviewXp = 20;

            var awardResult = await gamificationService.AwardXpAsync(new AwardXpRequest(
                UserId: demoUserId,
                SourceType: "srs_review",
                XpDelta: reviewXp,
                SourceId: vocabGuid,
                IdempotencyKey: request.IdempotencyKey,
                Metadata: null), ct);

            return Results.Ok(new
            {
                saved = true,
                xp = awardResult.AlreadyAwarded ? 0 : reviewXp
            });
        })
        .WithName("SubmitContentReview")
        .WithTags("Gamification")
        .WithSummary("Ghi nhận review từ vựng và cộng XP (Postman Sprint 1)")
        .AllowAnonymous();

        // 6. Content Quiz Attempts (Idempotent XP awarding tương thích Postman Sprint 1)
        app.MapPost("/api/v1/content/quiz-attempts", async (
            [FromBody] QuizAttemptSubmissionDto request,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            if (request.Answers == null || request.Answers.Count == 0)
            {
                return Results.BadRequest(new { error = "Invalid quiz attempt" });
            }

            var demoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            int correctCount = request.Answers.Count(a => a.IsCorrect);
            int xpToAward = request.Xp > 0 ? request.Xp : 20;

            var awardResult = await gamificationService.AwardXpAsync(new AwardXpRequest(
                UserId: demoUserId,
                SourceType: "clan_quiz",
                XpDelta: xpToAward,
                SourceId: null,
                IdempotencyKey: request.IdempotencyKey,
                Metadata: null), ct);

            return Results.Ok(new
            {
                saved = true,
                correct_answers = correctCount,
                xp = awardResult.AlreadyAwarded ? 0 : xpToAward
            });
        })
        .WithName("SubmitContentQuizAttempt")
        .WithTags("Gamification")
        .WithSummary("Ghi nhận quiz attempt và cộng XP (Postman Sprint 1)")
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}

public sealed record ReviewSubmissionDto(
    string? VocabularyId,
    string? Rating,
    int Quality,
    int Repetitions,
    decimal IntervalDays,
    decimal EaseFactor,
    DateTimeOffset NextDueAt,
    int TimeSpentMs,
    string? IdempotencyKey);

public sealed record QuizAnswerItem(
    string? QuestionId,
    string? SelectedKey,
    bool IsCorrect);

public sealed record QuizAttemptSubmissionDto(
    List<QuizAnswerItem>? Answers,
    int Xp,
    string? IdempotencyKey);

