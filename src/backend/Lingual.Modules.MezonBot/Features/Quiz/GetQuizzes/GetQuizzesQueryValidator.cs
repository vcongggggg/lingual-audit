using FluentValidation;

namespace Lingual.Modules.MezonBot.Features.Quiz.GetQuizzes;

public class GetQuizzesQueryValidator : AbstractValidator<GetQuizzesQuery>
{
    public GetQuizzesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Limit must not exceed 100 questions.");
    }
}
