using FluentValidation;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;

public class LoginWithMezonCommandValidator : AbstractValidator<LoginWithMezonCommand>
{
    public LoginWithMezonCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.Request.Code)
            .NotEmpty()
            .WithMessage("Authorization code is required.");

        RuleFor(x => x.Request.State)
            .NotEmpty()
            .WithMessage("OAuth state is required.");
    }
}
