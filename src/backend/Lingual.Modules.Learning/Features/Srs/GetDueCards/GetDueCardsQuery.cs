using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.SharedKernel.Shared;

namespace Lingual.Modules.Learning.Features.Srs.GetDueCards;

public record GetDueCardsQuery(Guid UserId) : IQuery<SrsDueCardsResponseDto>;
