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
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gamification")
            .WithTags("Gamification");

        // 1. Leaderboard API - Yêu cầu xác thực token
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
        .RequireAuthorization();

        // 2. Streak Info API - Yêu cầu xác thực token, lấy userId từ claims
        group.MapGet("/streak/me", async (
            HttpContext httpContext,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (!currentUserId.HasValue)
            {
                return Results.Unauthorized();
            }

            var streak = await gamificationService.GetUserStreakAsync(currentUserId.Value, ct);
            return Results.Ok(streak);
        })
        .WithName("GetMyStreak")
        .WithSummary("Lấy thông tin chuỗi ngày học Streak")
        .WithDescription("Trả về số ngày Streak hiện tại, kỷ lục, trạng thái Active hôm nay và số khiên Streak Freeze.")
        .Produces<UserStreakDto>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 3. Word Duel 1vs1 Endpoints (Sprint 3: SP3-01, SP3-02, SP3-04)
        group.MapPost("/duels/challenge", async (
            [FromBody] CreateDuelChallengeRequest request,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var match = await duelService.CreateChallengeAsync(request, ct);
            return Results.Ok(match);
        })
        .WithName("CreateDuelChallenge")
        .WithSummary("Tạo lời mời thách đấu hoặc trận đấu Word Duel 1vs1")
        .Produces<DuelMatchDto>(StatusCodes.Status200OK);

        group.MapGet("/duels/{matchId:guid}", async (
            Guid matchId,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var match = await duelService.GetMatchAsync(matchId, ct);
            return match != null ? Results.Ok(match) : Results.NotFound(new { message = "Không tìm thấy trận đấu." });
        })
        .WithName("GetDuelMatch")
        .WithSummary("Lấy thông tin chi tiết trận đấu Word Duel 1vs1")
        .Produces<DuelMatchDto>(StatusCodes.Status200OK);

        group.MapPost("/duels/{matchId:guid}/accept", async (
            Guid matchId,
            [FromQuery] Guid? userId,
            [FromQuery] string? userName,
            HttpContext httpContext,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var resolvedUserId = userId ?? GetCurrentUserId(httpContext) ?? Guid.NewGuid();
            var match = await duelService.AcceptChallengeAsync(matchId, resolvedUserId, userName, ct);
            return match != null ? Results.Ok(match) : Results.NotFound(new { message = "Không tìm thấy trận đấu." });
        })
        .WithName("AcceptDuelChallenge")
        .WithSummary("Chấp nhận lời thách đấu Word Duel 1vs1")
        .Produces<DuelMatchDto>(StatusCodes.Status200OK);

        group.MapGet("/duels/{matchId:guid}/rounds/{roundNumber:int}", async (
            Guid matchId,
            int roundNumber,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var question = await duelService.GetRoundQuestionAsync(matchId, roundNumber, ct);
            return question != null ? Results.Ok(question) : Results.NotFound(new { message = "Không tìm thấy câu hỏi cho vòng này." });
        })
        .WithName("GetDuelRoundQuestion")
        .WithSummary("Lấy câu hỏi đồng bộ của vòng đấu (không lộ đáp án đúng)")
        .Produces<DuelRoundQuestionDto>(StatusCodes.Status200OK);

        group.MapPost("/duels/{matchId:guid}/submit", async (
            Guid matchId,
            [FromBody] SubmitDuelAnswerRequest request,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var normalizedRequest = request with { MatchId = matchId };
            var result = await duelService.SubmitAnswerAsync(normalizedRequest, ct);
            return Results.Ok(result);
        })
        .WithName("SubmitDuelRoundAnswer")
        .WithSummary("Gửi đáp án vòng đấu Word Duel (Atomic JSONB Append & chấm điểm tốc độ)")
        .Produces<SubmitDuelAnswerResultDto>(StatusCodes.Status200OK);

        group.MapPost("/duels/{matchId:guid}/forfeit", async (
            Guid matchId,
            [FromQuery] Guid userId,
            [FromQuery] string? reason,
            [FromServices] IWordDuelService duelService,
            CancellationToken ct) =>
        {
            var match = await duelService.ForfeitMatchAsync(matchId, userId, reason ?? "forfeit", ct);
            return match != null ? Results.Ok(match) : Results.NotFound(new { message = "Không tìm thấy trận đấu." });
        })
        .WithName("ForfeitDuelMatch")
        .WithSummary("Xử lý đầu hàng hoặc xử thua do ngắt kết nối quá 10 giây")
        .Produces<DuelMatchDto>(StatusCodes.Status200OK);

        group.MapGet("/duels/history", async (
            [FromQuery] Guid? userId,
            [FromQuery] int limit = 10,
            HttpContext httpContext = null!,
            [FromServices] IWordDuelService duelService = null!,
            CancellationToken ct = default) =>
        {
            var targetUserId = userId ?? GetCurrentUserId(httpContext) ?? Guid.Empty;
            var history = await duelService.GetUserMatchHistoryAsync(targetUserId, limit, ct);
            return Results.Ok(history);
        })
        .WithName("GetDuelMatchHistory")
        .WithSummary("Lấy lịch sử các trận đấu Word Duel 1vs1")
        .Produces<IReadOnlyList<DuelMatchDto>>(StatusCodes.Status200OK);

        // LƯU Ý BẢO MẬT:
        // - Xóa endpoint public /xp/award để ngăn chặn client tự khai báo UserId/XpDelta.
        // - Xóa 3 route trùng /api/v1/content/leaderboard, /api/v1/content/reviews, /api/v1/content/quiz-attempts.
        //   (Tất cả được xử lý an toàn với server-side grading và JWT auth trong ContentController).
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
