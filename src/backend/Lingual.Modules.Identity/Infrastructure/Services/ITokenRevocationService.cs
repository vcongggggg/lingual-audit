namespace Lingual.Modules.Identity.Infrastructure.Services;

public interface ITokenRevocationService
{
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
