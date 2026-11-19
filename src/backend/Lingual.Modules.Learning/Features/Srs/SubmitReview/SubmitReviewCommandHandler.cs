using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Srs.SubmitReview;

public class SubmitReviewCommandHandler(ISrsService srsService)
    : ICommandHandler<SubmitReviewCommand, ReviewSubmissionResultDto>
{
    public async Task<ReviewSubmissionResultDto> Handle(
        SubmitReviewCommand command,
        CancellationToken cancellationToken)
    {
        return await srsService.SubmitReviewAsync(command.Request, command.UserId, cancellationToken);
    }
}
