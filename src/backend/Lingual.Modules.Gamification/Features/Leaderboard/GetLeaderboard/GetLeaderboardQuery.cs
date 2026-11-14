using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Leaderboard.GetLeaderboard;

public record GetLeaderboardQuery(
    int Limit = 20,
    string Type = "all-time",
    Guid? CurrentUserId = null) : IQuery<LeaderboardResponseDto>;
