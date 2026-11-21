using Lingual.Modules.Identity.Contracts.DTOs;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public interface IMezonAuthService
{
    Task<MezonCallbackResult> GetAuthorizeUrlAsync(string? redirectUri = null);
    Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request);
}
