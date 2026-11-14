using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Leaderboard.GetLeaderboard;

public class GetLeaderboardQueryHandler(ILeaderboardService leaderboardService)
    : IQueryHandler<GetLeaderboardQuery, LeaderboardResponseDto>
{
    public async Task<LeaderboardResponseDto> Handle(
        GetLeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        return await leaderboardService.GetLeaderboardAsync(
            query.Limit,
            query.Type,
            query.CurrentUserId,
            cancellationToken);
    }
}
