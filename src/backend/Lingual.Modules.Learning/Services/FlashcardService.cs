using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.Modules.Learning.Domain;
using Lingual.Modules.Learning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Learning.Services;

public class FlashcardService : IFlashcardService
{
    private readonly LearningDbContext _dbContext;
    private readonly ILogger<FlashcardService> _logger;

    public FlashcardService(LearningDbContext dbContext, ILogger<FlashcardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SrsCardDto> CreateOrBookmarkFlashcardAsync(
        Guid userId,
        string term,
        string meaning,
        CancellationToken cancellationToken = default)
    {
        var cleanTerm = (term ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanTerm))
        {
            throw new ArgumentException("Thuật ngữ từ vựng không được để trống.", nameof(term));
        }

        var normalized = cleanTerm.ToLowerInvariant();
        var cleanMeaning = string.IsNullOrWhiteSpace(meaning) ? cleanTerm : meaning.Trim();

        var vocab = await _dbContext.VocabularyItems
            .FirstOrDefaultAsync(v => v.NormalizedTerm == normalized, cancellationToken);

        if (vocab == null)
        {
            vocab = new VocabularyItem
            {
                Id = Guid.NewGuid(),
                Term = cleanTerm,
                NormalizedTerm = normalized,
                Meaning = cleanMeaning,
                SourceLanguage = "en",
                TargetLanguage = "vi",
                Status = "published",
                CreatedBy = userId
            };
            _dbContext.VocabularyItems.Add(vocab);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("📚 [FlashcardService] Đã tạo mới VocabularyItem: {Term} ({VocabId})", cleanTerm, vocab.Id);
        }

        var card = await _dbContext.SrsCards
            .FirstOrDefaultAsync(c => c.UserId == userId && c.VocabularyId == vocab.Id, cancellationToken);

        if (card == null)
        {
            // Khởi tạo thẻ Spaced Repetition (SM-2): interval 1 ngày, reps 0, ease 2.50
            card = new SrsCard
            {
                UserId = userId,
                VocabularyId = vocab.Id,
                Repetitions = 0,
                IntervalDays = 1.0m,
                EaseFactor = 2.50m,
                NextDueAt = DateTime.UtcNow.AddDays(1),
                Lapses = 0,
                ReviewHistory = "[]"
            };
            _dbContext.SrsCards.Add(card);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("🔖 [FlashcardService] Đã tạo SrsCard cho User {UserId} với từ {Term}", userId, cleanTerm);
        }

        return new SrsCardDto(
            card.UserId,
            vocab.Id,
            vocab.Term,
            vocab.Meaning,
            card.Repetitions,
            card.IntervalDays,
            card.EaseFactor,
            card.NextDueAt,
            card.LastReviewedAt
        );
    }

    public async Task<List<SrsCardDto>> GetDueCardsAsync(
        Guid userId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cards = await _dbContext.SrsCards
            .Include(c => c.VocabularyItem)
            .Where(c => c.UserId == userId && c.NextDueAt <= now && c.SuspendedAt == null)
            .OrderBy(c => c.NextDueAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return cards.Select(c => new SrsCardDto(
            c.UserId,
            c.VocabularyId,
            c.VocabularyItem?.Term ?? "Unknown",
            c.VocabularyItem?.Meaning ?? string.Empty,
            c.Repetitions,
            c.IntervalDays,
            c.EaseFactor,
            c.NextDueAt,
            c.LastReviewedAt
        )).ToList();
    }

    public async Task<List<Guid>> GetUsersWithDueCardsAsync(
        DateTime dueBefore,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SrsCards
            .Where(c => c.NextDueAt <= dueBefore && c.SuspendedAt == null)
            .Select(c => c.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}

