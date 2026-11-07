using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Services;

public interface IXpService
{
    Task<AwardXpResult> AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken = default);

    Task<int> GetTotalXpAsync(Guid userId, CancellationToken cancellationToken = default);
}

