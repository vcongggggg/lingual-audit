using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.MezonBot.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.MezonBot.Features.Quiz.GetQuizzes;

public class GetQuizzesEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route GET /api/v1/content/quizzes được phục vụ bởi ContentController (ẩn đáp án chống F12, lọc mode/difficulty/questionType).
        // Endpoint này giữ route chuẩn /api/v1/quizzes.
        app.MapGet("/api/v1/quizzes", async (
            [FromQuery] int limit = 10,
            [FromQuery] string? level = null,
            [FromServices] ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetQuizzesQuery(limit, level), ct);
            return Results.Ok(result);
        })
        .WithName("GetQuizzesAlias")
        .WithTags("Quiz")
        .WithSummary("Lấy ngân hàng câu hỏi trắc nghiệm")
        .Produces<QuizListResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }
}
