using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;


public record LoginWithMezonCommand(MezonAuthRequest Request) : ICommand<MezonCallbackResult>;

