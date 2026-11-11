using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Services;

public class GamificationService(
    IXpService xpService,
    IStreakService streakService,
    ILeaderboardService leaderboardService) : IGamificationService
{
    public Task<AwardXpResult> AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken = default)
    {
        return xpService.AwardXpAsync(request, cancellationToken);
    }

    public Task<UserStreakDto> GetUserStreakAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.GetStreakAsync(userId, cancellationToken);
    }

    public Task<DailyCheckinResultDto> CheckInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.CheckInAsync(userId, cancellationToken);
    }

    public Task<StreakFreezeResultDto> UseStreakFreezeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.UseStreakFreezeAsync(userId, cancellationToken);
    }

    public Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        return leaderboardService.GetLeaderboardAsync(limit, type, currentUserId, cancellationToken);
    }
}
