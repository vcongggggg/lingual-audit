using FluentValidation;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;

public class LoginWithMezonCommandValidator : AbstractValidator<LoginWithMezonCommand>
{
    public LoginWithMezonCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.Request)
            .Must(r => !string.IsNullOrWhiteSpace(r.EffectiveToken) || !string.IsNullOrWhiteSpace(r.MezonUserId))
            .WithMessage("Either an authentication token or a Mezon user ID is required.");
    }
}
