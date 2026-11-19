using Lingual.Modules.Learning.Contracts.DTOs;

namespace Lingual.Modules.Learning.Contracts;

public interface ISrsService
{
    Task<ReviewSubmissionResultDto> SubmitReviewAsync(
        ReviewSubmissionRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<SrsDueCardsResponseDto> GetDueCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
