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
        // Route /api/v1/content/vocabulary được phục vụ bởi ContentController (hỗ trợ lọc q, topic, chapters, groups).
        // Endpoint này giữ route chuẩn module Learning: /api/v1/learning/vocabulary.
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
