using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;


public class LoginWithMezonCommandHandler(IMezonAuthService mezonAuthService)
    : ICommandHandler<LoginWithMezonCommand, MezonCallbackResult>
{
    public async Task<MezonCallbackResult> Handle(
        LoginWithMezonCommand command, 
        CancellationToken cancellationToken)
    {
        return await mezonAuthService.HandleCallbackAsync(command.Request);
    }
}
