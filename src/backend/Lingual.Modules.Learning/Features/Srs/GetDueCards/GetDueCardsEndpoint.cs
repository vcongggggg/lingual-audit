using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Learning.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Learning.Features.Srs.GetDueCards;

public class GetDueCardsEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // GET /api/v1/learning/srs/due (Hàng đợi thẻ đến hạn ôn tập)
        app.MapGet("/api/v1/learning/srs/due", async (
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext) ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            var result = await sender.Send(new GetDueCardsQuery(userId), ct);
            return Results.Ok(result);
        })
        .WithName("GetSrsDueCards")
        .WithTags("Learning")
        .WithSummary("Lấy danh sách các thẻ từ vựng đến hạn ôn tập")
        .Produces<SrsDueCardsResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
