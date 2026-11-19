using FluentValidation;

namespace Lingual.Modules.MezonBot.Features.Quiz.SubmitQuizAttempt;

public class SubmitQuizAttemptCommandValidator : AbstractValidator<SubmitQuizAttemptCommand>
{
    public SubmitQuizAttemptCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("Request is required.");

        RuleFor(x => x.Request.Answers)
            .NotEmpty().WithMessage("Answers list cannot be empty.");
    }
}
