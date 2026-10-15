using Lingual.Modules.Identity.Contracts.DTOs;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public interface IMezonAuthService
{
    Task<MezonCallbackResult> GetAuthorizeUrlAsync();
    Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request);
}
