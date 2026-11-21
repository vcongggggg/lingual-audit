using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Srs.SubmitReview;

public record SubmitReviewCommand(
    ReviewSubmissionRequestDto Request,
    Guid? UserId) : ICommand<ReviewSubmissionResultDto>;
