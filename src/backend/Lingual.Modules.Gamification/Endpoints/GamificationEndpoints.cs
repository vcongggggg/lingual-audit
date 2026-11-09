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
