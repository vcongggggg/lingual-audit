using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Streak.GetUserStreak;

public class GetUserStreakQueryHandler(IStreakService streakService)
    : IQueryHandler<GetUserStreakQuery, UserStreakDto>
{
    public async Task<UserStreakDto> Handle(
        GetUserStreakQuery query,
        CancellationToken cancellationToken)
    {
        return await streakService.GetStreakAsync(query.UserId, cancellationToken);
    }
}
