using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Learning.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Learning.Features.Vocabulary.GetVocabulary;

public class GetVocabularyEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 1. Content Vocabulary (Tương thích Frontend Flashcard và Postman Sprint 1)
        app.MapGet("/api/v1/content/vocabulary", async (
            [FromQuery] int limit = 50,
            [FromQuery(Name = "source_language")] string sourceLanguage = "en",
            [FromQuery(Name = "target_language")] string targetLanguage = "vi",
            [FromQuery] string? level = null,
            [FromServices] ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var query = new GetVocabularyQuery(limit, sourceLanguage, targetLanguage, level);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetContentVocabulary")
        .WithTags("Vocabulary")
        .WithSummary("Lấy danh sách từ vựng theo ngôn ngữ (A1-A2)")
        .Produces<VocabularyListResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 2. Alias: /api/v1/learning/vocabulary
        app.MapGet("/api/v1/learning/vocabulary", async (
            [FromQuery] int limit = 50,
            [FromQuery(Name = "source_language")] string sourceLanguage = "en",
            [FromQuery(Name = "target_language")] string targetLanguage = "vi",
            [FromQuery] string? level = null,
            [FromServices] ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var query = new GetVocabularyQuery(limit, sourceLanguage, targetLanguage, level);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetLearningVocabulary")
        .WithTags("Learning")
        .WithSummary("Lấy danh sách từ vựng theo module Learning")
        .Produces<VocabularyListResponseDto>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }
}
