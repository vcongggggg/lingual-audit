using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.MezonBot.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.MezonBot.Features.Quiz.SubmitQuizAttempt;

public class SubmitQuizAttemptEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route POST /api/v1/content/quiz-attempts được phục vụ bởi ContentController ([Authorize], server tự chấm đáp án).
        // Endpoint này giữ route chuẩn /api/v1/quizzes/attempts.
        app.MapPost("/api/v1/quizzes/attempts", async (
            [FromBody] QuizAttemptRequestDto request,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            if (request.Answers == null || request.Answers.Count == 0)
            {
                return Results.BadRequest(new { error = "Invalid quiz attempt" });
            }

            var userId = GetCurrentUserId(httpContext);
            var result = await sender.Send(new SubmitQuizAttemptCommand(request, userId), ct);
            return Results.Ok(result);
        })
        .WithName("SubmitQuizzesAttemptsAlias")
        .WithTags("Quiz")
        .WithSummary("Ghi nhận lượt thi quiz")
        .Produces<QuizAttemptResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
