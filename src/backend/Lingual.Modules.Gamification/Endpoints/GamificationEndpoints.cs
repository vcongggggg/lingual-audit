using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
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
            return Results.Ok(new
            {
                updated_at = result.UpdatedAt,
                entries = result.Entries.Select(e => new
                {
                    rank = e.Rank,
                    user_id = e.UserId,
                    name = e.Name,
                    xp = e.Xp
                })
            });
        })
        .WithName("GetContentLeaderboardAlias")
        .WithTags("Gamification")
        .WithSummary("Bảng xếp hạng (Alias tương thích Postman & WebApp)")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}

