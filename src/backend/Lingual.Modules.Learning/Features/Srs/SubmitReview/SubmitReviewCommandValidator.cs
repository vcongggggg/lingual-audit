using FluentValidation;

namespace Lingual.Modules.Learning.Features.Srs.SubmitReview;

public class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("Request is required.");

        RuleFor(x => x.Request.VocabularyId)
            .NotEmpty().WithMessage("Vocabulary ID is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Vocabulary ID must be a valid GUID.");

        RuleFor(x => x.Request.Quality)
            .InclusiveBetween(0, 5).WithMessage("Quality score must be between 0 and 5.");

        RuleFor(x => x.Request.Repetitions)
            .GreaterThanOrEqualTo(0).WithMessage("Repetitions must be non-negative.");

        RuleFor(x => x.Request.IntervalDays)
            .GreaterThanOrEqualTo(0).WithMessage("Interval days must be non-negative.");

        RuleFor(x => x.Request.EaseFactor)
            .GreaterThanOrEqualTo(1.3m).WithMessage("Ease factor must be at least 1.3.");
    }
}
