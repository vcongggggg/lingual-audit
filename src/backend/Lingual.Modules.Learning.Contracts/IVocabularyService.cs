using Lingual.Modules.Learning.Contracts.DTOs;

namespace Lingual.Modules.Learning.Contracts;

public interface IVocabularyService
{
    Task<VocabularyListResponseDto> GetVocabularyAsync(
        VocabularyQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
