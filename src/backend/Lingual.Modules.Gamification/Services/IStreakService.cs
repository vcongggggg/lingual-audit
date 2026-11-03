using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Services;

public interface IStreakService
{
    Task<UserStreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserStreakDto> RecordXpActivityAsync(
        Guid userId,
        int xpDelta,
        int studySeconds = 0,
        int wordsCount = 0,
        CancellationToken cancellationToken = default);
}

