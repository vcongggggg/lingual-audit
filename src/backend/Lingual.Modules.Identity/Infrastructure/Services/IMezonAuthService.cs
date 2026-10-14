using Lingual.Modules.Identity.Contracts.DTOs;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public record MezonUserInfo(
    string MezonUserId,
    string DisplayName,
    string? AvatarUrl
);

public interface IMezonAuthService
{
    MezonAuthorizeUrlDto GetAuthorizeUrl(string? customRedirectUri = null);
    Task<MezonUserInfo?> ValidateAndGetUserInfoAsync(LoginWithMezonRequest request, CancellationToken cancellationToken = default);
}
