using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Services;

public interface ILeaderboardService
{
    Task IncrementXpAsync(string userId, double xpDelta, CancellationToken cancellationToken = default);

    Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task FinalizeWeeklyLeaderboardAsync(CancellationToken cancellationToken = default);
}

