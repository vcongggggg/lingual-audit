using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Leaderboard.GetClanLeaderboard;

public class GetClanLeaderboardQueryHandler(ILeaderboardService leaderboardService)
    : IQueryHandler<GetClanLeaderboardQuery, ClanLeaderboardResponseDto>
{
    public async Task<ClanLeaderboardResponseDto> Handle(
        GetClanLeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        return await leaderboardService.GetClanLeaderboardAsync(
            query.Limit,
            query.Type,
            query.CurrentUserId,
            cancellationToken);
    }
}
