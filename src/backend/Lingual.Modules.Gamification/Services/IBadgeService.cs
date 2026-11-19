using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Services;

public interface IBadgeService
{
    Task<BadgesResponseDto> GetBadgesAsync(Guid userId, CancellationToken cancellationToken = default);
}
