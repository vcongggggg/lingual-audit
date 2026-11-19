using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Leaderboard.GetClanLeaderboard;

public record GetClanLeaderboardQuery(
    int Limit = 20,
    string Type = "all-time",
    Guid? CurrentUserId = null) : IQuery<ClanLeaderboardResponseDto>;
