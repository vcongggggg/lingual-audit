using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Contracts;

public interface IGamificationService
{
    Task<AwardXpResult> AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken = default);

    Task<UserStreakDto> GetUserStreakAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);
}

