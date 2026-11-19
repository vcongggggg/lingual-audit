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
        // 2. User Profile & Achievements Summary API
        group.MapGet("/profile/summary", async (
            [FromQuery] Guid? userId,
            HttpContext httpContext,
            [FromServices] IGamificationService gamificationService,
            CancellationToken ct) =>
        {
            var targetUserId = userId ?? GetCurrentUserId(httpContext);
            var result = await gamificationService.GetUserProfileSummaryAsync(targetUserId, ct);
            return Results.Ok(result);
        })
        .WithName("GetUserProfileSummary")
        .WithSummary("Lấy thông tin tổng quan hồ sơ cá nhân và thành tích gamification")
        .WithDescription("Trả về cấp độ, điểm XP, chuỗi Streak, danh sách huy hiệu và chỉ số học tập.")
        .Produces<UserProfileSummaryDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
