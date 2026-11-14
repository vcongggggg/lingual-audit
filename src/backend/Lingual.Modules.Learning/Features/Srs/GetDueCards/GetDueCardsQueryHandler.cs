using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Srs.GetDueCards;

public class GetDueCardsQueryHandler(ISrsService srsService)
    : IQueryHandler<GetDueCardsQuery, SrsDueCardsResponseDto>
{
    public async Task<SrsDueCardsResponseDto> Handle(
        GetDueCardsQuery query,
        CancellationToken cancellationToken)
    {
        return await srsService.GetDueCardsAsync(query.UserId, cancellationToken);
    }
}
