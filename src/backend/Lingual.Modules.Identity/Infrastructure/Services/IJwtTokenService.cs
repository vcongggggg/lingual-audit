using Lingual.Modules.Identity.Domain;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) GenerateToken(User user);
}
