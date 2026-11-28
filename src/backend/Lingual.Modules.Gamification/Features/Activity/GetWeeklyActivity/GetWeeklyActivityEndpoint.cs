using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Features.Activity.GetWeeklyActivity;

public class GetWeeklyActivityEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/gamification/activity/weekly", async (
            HttpContext httpContext,
            [FromQuery] int? days,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext) ?? Guid.Empty;
            var result = await sender.Send(new GetWeeklyActivityQuery(userId, days), ct);
            return Results.Ok(result);
        })
        .WithName("GetWeeklyActivity")
        .WithTags("Gamification")
        .WithSummary("Lấy dữ liệu tiến độ học tập 7 ngày trong tuần")
        .Produces<WeeklyActivityResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
