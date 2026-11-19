using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Vocabulary.GetVocabulary;

public class GetVocabularyQueryHandler(IVocabularyService vocabularyService)
    : IQueryHandler<GetVocabularyQuery, VocabularyListResponseDto>
{
    public async Task<VocabularyListResponseDto> Handle(
        GetVocabularyQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = new VocabularyQueryParameters(
            query.Limit,
            query.SourceLanguage,
            query.TargetLanguage,
            query.Level);

        return await vocabularyService.GetVocabularyAsync(parameters, cancellationToken);
    }
}
