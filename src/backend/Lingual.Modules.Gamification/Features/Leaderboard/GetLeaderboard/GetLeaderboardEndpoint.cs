using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Features.Leaderboard.GetLeaderboard;

public class GetLeaderboardEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/gamification/leaderboard", async (
            [FromQuery] int limit = 20,
            [FromQuery] string type = "all-time",
            HttpContext httpContext = null!,
            [FromServices] ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetCurrentUserId(httpContext);
            var result = await sender.Send(new GetLeaderboardQuery(limit, type, userId), ct);
            return Results.Ok(result);
        })
        .WithName("GetLeaderboard")
        .WithTags("Gamification")
        .WithSummary("Lấy bảng xếp hạng cá nhân theo khung thời gian (Redis Sorted Set)")
        .WithDescription("Hỗ trợ type=weekly, monthly, all-time.")
        .Produces<LeaderboardResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
