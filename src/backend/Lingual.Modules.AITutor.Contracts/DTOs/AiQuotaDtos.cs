namespace Lingual.Modules.AITutor.Contracts.DTOs;

public record AiQuotaStatusDto(
    int DailyFreeLimit,
    int DailyUsed,
    int DailyRemaining,
    int BonusCredits,
    int TotalAvailable,
    int DailyBonusEarned,
    int DailyBonusCap,
    DateTime NextResetAtUtc,
    bool IsGuest = false
);

public record AiQuotaConsumeResult(
    bool Allowed,
    bool AlreadyProcessed,
    bool ConsumedFromDailyFree,
    string IdempotencyKey,
    string? ErrorCode,
    string? Message,
    AiQuotaStatusDto Quota
);

public record AiQuotaGrantResult(
    bool Granted,
    int CreditsAdded,
    bool AlreadyGranted,
    bool DailyCapReached,
    AiQuotaStatusDto Quota
);
