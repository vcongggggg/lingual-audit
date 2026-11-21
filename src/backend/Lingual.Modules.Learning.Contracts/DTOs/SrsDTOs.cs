using System.Text.Json.Serialization;

namespace Lingual.Modules.Learning.Contracts.DTOs;

public sealed record ReviewSubmissionRequestDto(
    [property: JsonPropertyName("vocabularyId")] string? VocabularyId,
    [property: JsonPropertyName("rating")] string? Rating,
    [property: JsonPropertyName("quality")] int Quality,
    [property: JsonPropertyName("repetitions")] int Repetitions,
    [property: JsonPropertyName("intervalDays")] decimal IntervalDays,
    [property: JsonPropertyName("easeFactor")] decimal EaseFactor,
    [property: JsonPropertyName("nextDueAt")] DateTimeOffset NextDueAt,
    [property: JsonPropertyName("timeSpentMs")] int TimeSpentMs,
    [property: JsonPropertyName("idempotencyKey")] string? IdempotencyKey);

public sealed record ReviewSubmissionResultDto(
    [property: JsonPropertyName("saved")] bool Saved,
    [property: JsonPropertyName("xp")] int Xp,
    [property: JsonPropertyName("nextDueAt")] DateTimeOffset? NextDueAt = null);

public sealed record SrsDueCardDto(
    [property: JsonPropertyName("vocabulary_id")] Guid VocabularyId,
    [property: JsonPropertyName("term")] string Term,
    [property: JsonPropertyName("meaning")] string Meaning,
    [property: JsonPropertyName("ipa")] string? Ipa,
    [property: JsonPropertyName("repetitions")] int Repetitions,
    [property: JsonPropertyName("interval_days")] decimal IntervalDays,
    [property: JsonPropertyName("ease_factor")] decimal EaseFactor,
    [property: JsonPropertyName("next_due_at")] DateTime NextDueAt);

public sealed record SrsDueCardsResponseDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("cards")] IReadOnlyList<SrsDueCardDto> Cards);
