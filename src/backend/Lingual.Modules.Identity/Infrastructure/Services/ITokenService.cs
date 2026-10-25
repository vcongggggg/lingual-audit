using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Domain;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiration();
}
