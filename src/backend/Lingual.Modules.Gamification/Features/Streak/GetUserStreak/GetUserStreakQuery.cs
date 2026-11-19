using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Streak.GetUserStreak;

public record GetUserStreakQuery(Guid UserId) : IQuery<UserStreakDto>;
