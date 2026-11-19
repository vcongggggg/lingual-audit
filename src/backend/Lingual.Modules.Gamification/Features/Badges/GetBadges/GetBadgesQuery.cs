using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Badges.GetBadges;

public record GetBadgesQuery(Guid UserId) : IQuery<BadgesResponseDto>;
