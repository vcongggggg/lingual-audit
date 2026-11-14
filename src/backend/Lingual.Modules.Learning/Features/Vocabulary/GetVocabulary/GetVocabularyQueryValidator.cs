using FluentValidation;

namespace Lingual.Modules.Learning.Features.Vocabulary.GetVocabulary;

public class GetVocabularyQueryValidator : AbstractValidator<GetVocabularyQuery>
{
    public GetVocabularyQueryValidator()
    {
        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0.")
            .LessThanOrEqualTo(500).WithMessage("Limit must not exceed 500 items.");

        RuleFor(x => x.SourceLanguage)
            .NotEmpty().WithMessage("Source language is required.");

        RuleFor(x => x.TargetLanguage)
            .NotEmpty().WithMessage("Target language is required.");
    }
}
