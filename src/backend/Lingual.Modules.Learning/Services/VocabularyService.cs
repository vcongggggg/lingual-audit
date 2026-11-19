using System.Text.Json;
using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.Modules.Learning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Learning.Services;

public class VocabularyService(
    LearningDbContext dbContext,
    ILogger<VocabularyService> logger) : IVocabularyService
{
    public async Task<VocabularyListResponseDto> GetVocabularyAsync(
        VocabularyQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(parameters.Limit, 1, 500);

        try
        {
            var rawWords = await dbContext.VocabularyItems
                .AsNoTracking()
                .Where(v => v.Status == "published" &&
                            v.SourceLanguage == parameters.SourceLanguage &&
                            v.TargetLanguage == parameters.TargetLanguage)
                .OrderBy(v => v.NormalizedTerm)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var words = rawWords.Select(v =>
            {
                object examplesObj;
                try
                {
                    examplesObj = JsonSerializer.Deserialize<JsonElement>(v.Examples);
                }
                catch
                {
                    examplesObj = Array.Empty<object>();
                }

                return new VocabularyItemDto(
                    Id: v.Id.ToString(),
                    Term: v.Term,
                    NormalizedTerm: v.NormalizedTerm,
                    SourceLanguage: v.SourceLanguage,
                    TargetLanguage: v.TargetLanguage,
                    Ipa: v.Ipa,
                    PartOfSpeech: v.PartOfSpeech,
                    Meaning: v.Meaning,
                    PronunciationAudioUrl: v.PronunciationAudioUrl,
                    ImageUrl: v.ImageUrl,
                    Examples: examplesObj);
            }).ToList();

            return new VocabularyListResponseDto(words.Count, words);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi truy vấn danh sách từ vựng từ cơ sở dữ liệu");
            return new VocabularyListResponseDto(0, Array.Empty<VocabularyItemDto>());
        }
    }
}
