namespace Lingual.Modules.Learning.Contracts.DTOs;

public record SrsCardDto(
    Guid UserId,
    Guid VocabularyId,
    string Term,
    string Meaning,
    int Repetitions,
    decimal IntervalDays,
    decimal EaseFactor,
    DateTime NextDueAt,
    DateTime? LastReviewedAt
);

