using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Gamification.Features.Activity.GetWeeklyActivity;

public class GetWeeklyActivityQueryHandler(IStreakService streakService)
    : IQueryHandler<GetWeeklyActivityQuery, WeeklyActivityResponseDto>
{
    public async Task<WeeklyActivityResponseDto> Handle(
        GetWeeklyActivityQuery query,
        CancellationToken cancellationToken)
    {
        return await streakService.GetWeeklyActivityAsync(query.UserId, query.Days, cancellationToken);
    }
}
