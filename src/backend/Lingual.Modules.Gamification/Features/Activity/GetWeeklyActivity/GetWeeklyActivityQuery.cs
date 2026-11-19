using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Activity.GetWeeklyActivity;

public record GetWeeklyActivityQuery(Guid UserId) : IQuery<WeeklyActivityResponseDto>;
