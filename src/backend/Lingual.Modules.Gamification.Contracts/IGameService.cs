using System;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Contracts;

public interface IGameService
{
    Task<GameDataResponseDto> GetGameDataAsync(string? type, string? difficulty, Guid userId, CancellationToken ct = default);

    Task<GameSubmitResultDto> SubmitGameAsync(GameSubmitRequestDto request, Guid userId, string? displayName = null, CancellationToken ct = default);

    Task<GameLeaderboardResponseDto> GetLeaderboardAsync(string? type, int limit = 20, CancellationToken ct = default);

    Task<UserGameStatsResponseDto> GetUserStatsAsync(Guid userId, CancellationToken ct = default);
}
