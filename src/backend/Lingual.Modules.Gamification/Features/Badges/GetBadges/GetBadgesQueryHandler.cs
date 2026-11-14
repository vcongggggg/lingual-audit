using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Badges.GetBadges;

public class GetBadgesQueryHandler(IBadgeService badgeService)
    : IQueryHandler<GetBadgesQuery, BadgesResponseDto>
{
    public async Task<BadgesResponseDto> Handle(
        GetBadgesQuery query,
        CancellationToken cancellationToken)
    {
        return await badgeService.GetBadgesAsync(query.UserId, cancellationToken);
    }
}
