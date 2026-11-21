using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Vocabulary.GetVocabulary;

public record GetVocabularyQuery(
    int Limit = 50,
    string SourceLanguage = "en",
    string TargetLanguage = "vi",
    string? Level = null) : IQuery<VocabularyListResponseDto>;
