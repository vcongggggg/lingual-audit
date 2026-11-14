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

        // 1. Award XP API (Nội bộ / Admin / Learning integration)
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

        // 2. Content Leaderboard Alias (Tương thích 100% Postman Sprint 1 & Web App)
        app.MapGet("/api/v1/content/leaderboard", async (
            [FromQuery] int limit = 20,
            [FromQuery] string type = "all-time",
            [FromQuery] string scope = "personal",
            HttpContext httpContext = null!,
            [FromServices] IGamificationService gamificationService = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetCurrentUserId(httpContext);

            if (scope.Equals("clan", StringComparison.OrdinalIgnoreCase))
            {
                var clanResult = await gamificationService.GetClanLeaderboardAsync(limit, type, userId, ct);
                return Results.Ok(new
                {
                    updated_at = clanResult.UpdatedAt,
                    entries = clanResult.Entries.Select(e => new
                    {
                        rank = e.Rank,
                        id = e.Id,
                        user_id = e.Id,
                        name = e.Name,
                        xp = e.Xp,
                        tag = e.Tag,
                        avatar = e.Avatar,
                        level = e.Level,
                        words_learned = e.WordsLearned,
                        member_count = e.MemberCount,
                        is_current_user = e.IsCurrentUser
                    })
                });
            }

            var result = await gamificationService.GetLeaderboardAsync(limit, type, userId, ct);
            return Results.Ok(new
            {
                updated_at = result.UpdatedAt,
                entries = result.Entries.Select(e => new
                {
                    rank = e.Rank,
                    user_id = e.UserId,
                    name = e.Name,
                    xp = e.Xp,
                    avatar = e.AvatarUrl ?? "",
                    level = e.Level,
                    words_learned = e.WordsLearned,
                    streak_days = e.StreakDays,
                    is_current_user = e.UserId == DemoUserKey || (userId.HasValue && e.UserId == userId.Value.ToString())
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

