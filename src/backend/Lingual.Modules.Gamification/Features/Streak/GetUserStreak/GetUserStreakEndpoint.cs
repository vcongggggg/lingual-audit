using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Features.Streak.GetUserStreak;

public class GetUserStreakEndpoint : IEndpointBase
{
    private static readonly Guid DefaultDemoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gamification/streak")
            .WithTags("Gamification");

        group.MapGet("/me", async (
            [FromQuery] Guid? userId,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var targetUserId = userId ?? GetCurrentUserId(httpContext) ?? DefaultDemoUserId;
            var result = await sender.Send(new GetUserStreakQuery(targetUserId), ct);
            return Results.Ok(result);
        })
        .WithName("GetMyStreak")
        .WithSummary("Lấy thông tin chuỗi ngày học Streak")
        .WithDescription("Trả về số ngày Streak hiện tại, kỷ lục, trạng thái Active hôm nay và số khiên Streak Freeze.")
        .Produces<UserStreakDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/{userId:guid}", async (
            Guid userId,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserStreakQuery(userId), ct);
            return Results.Ok(result);
        })
        .WithName("GetUserStreakById")
        .WithSummary("Lấy thông tin chuỗi ngày học Streak theo User ID")
        .Produces<UserStreakDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
