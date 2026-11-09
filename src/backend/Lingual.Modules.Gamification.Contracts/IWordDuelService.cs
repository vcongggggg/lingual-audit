using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Contracts;

public interface IWordDuelService
{
    Task<DuelMatchDto> CreateChallengeAsync(
        CreateDuelChallengeRequest request,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> AcceptChallengeAsync(
        Guid matchId,
        Guid acceptorId,
        string? acceptorName = null,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> DeclineChallengeAsync(
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> ExpireNoShowMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> StartMatchInProgressAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<DuelRoundQuestionDto?> GetRoundQuestionAsync(
        Guid matchId,
        int roundNumber,
        CancellationToken cancellationToken = default);

    Task<SubmitDuelAnswerResultDto> SubmitAnswerAsync(
        SubmitDuelAnswerRequest request,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> ForfeitMatchAsync(
        Guid matchId,
        Guid forfeitingUserId,
        string reason = "forfeit",
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> CompleteMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<DuelMatchDto?> GetMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuelMatchDto>> GetUserMatchHistoryAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

