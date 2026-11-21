namespace Lingual.Modules.Gamification.Contracts.DTOs;

public sealed record AwardXpRequest(
    Guid UserId,
    string SourceType,
    int XpDelta,
    Guid? SourceId = null,
    string? IdempotencyKey = null,
    string? Metadata = null);

