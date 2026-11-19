using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Features.Badges.GetBadges;

public class GetBadgesEndpoint : IEndpointBase
{
    private static readonly Guid DefaultDemoUserId = Guid.Parse("4943737a-792a-4fca-8315-df58d25c2156");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/gamification/badges", async (
            [FromQuery] Guid? userId,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var targetUserId = userId ?? GetCurrentUserId(httpContext);
            if (!targetUserId.HasValue)
            {
                var emptyResult = await sender.Send(new GetBadgesQuery(Guid.Empty), ct);
                return Results.Ok(emptyResult);
            }
            var result = await sender.Send(new GetBadgesQuery(targetUserId.Value), ct);
            return Results.Ok(result);
        })
        .WithName("GetBadges")
        .WithTags("Gamification")
        .WithSummary("Lấy danh sách huy hiệu và tiến độ mở khóa")
        .Produces<BadgesResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
