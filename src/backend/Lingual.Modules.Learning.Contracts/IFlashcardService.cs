using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Learning.Contracts.DTOs;

namespace Lingual.Modules.Learning.Contracts;

public interface IFlashcardService
{
    Task<SrsCardDto> CreateOrBookmarkFlashcardAsync(
        Guid userId,
        string term,
        string meaning,
        CancellationToken cancellationToken = default);

    Task<List<SrsCardDto>> GetDueCardsAsync(
        Guid userId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetUsersWithDueCardsAsync(
        DateTime dueBefore,
        CancellationToken cancellationToken = default);
}

