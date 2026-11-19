using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Learning.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Learning.Features.Srs.SubmitReview;

public class SubmitReviewEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 1. Ghi nhận ôn tập từ vựng & Tính toán SM-2 (Postman Sprint 1 & FE /learn)
        app.MapPost("/api/v1/content/reviews", async (
            [FromBody] ReviewSubmissionRequestDto request,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.VocabularyId) ||
                !Guid.TryParse(request.VocabularyId, out _) ||
                request.Quality < 0 || request.Quality > 5 ||
                request.Repetitions < 0 || request.IntervalDays < 0 || request.EaseFactor < 1.3m)
            {
                return Results.BadRequest(new { error = "Invalid review payload" });
            }

            var userId = GetCurrentUserId(httpContext);
            var result = await sender.Send(new SubmitReviewCommand(request, userId), ct);
            return Results.Ok(result);
        })
        .WithName("SubmitContentReview")
        .WithTags("Learning")
        .WithSummary("Ghi nhận review từ vựng, tính SM-2 và cộng XP")
        .Produces<ReviewSubmissionResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // 2. Alias: POST /api/v1/learning/srs/review
        app.MapPost("/api/v1/learning/srs/review", async (
            [FromBody] ReviewSubmissionRequestDto request,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.VocabularyId) ||
                !Guid.TryParse(request.VocabularyId, out _) ||
                request.Quality < 0 || request.Quality > 5 ||
                request.Repetitions < 0 || request.IntervalDays < 0 || request.EaseFactor < 1.3m)
            {
                return Results.BadRequest(new { error = "Invalid review payload" });
            }

            var userId = GetCurrentUserId(httpContext);
            var result = await sender.Send(new SubmitReviewCommand(request, userId), ct);
            return Results.Ok(result);
        })
        .WithName("SubmitLearningSrsReview")
        .WithTags("Learning")
        .WithSummary("Alias ghi nhận review thẻ nhớ SRS")
        .Produces<ReviewSubmissionResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
