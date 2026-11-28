using Lingual.Modules.AITutor.Contracts.DTOs;

namespace Lingual.Modules.AITutor.Contracts;

public interface IAiQuotaService
{
    Task<AiQuotaStatusDto> GetQuotaAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AiQuotaConsumeResult> TryConsumeTurnAsync(
        Guid userId,
        string reason,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<AiQuotaStatusDto> RefundTurnAsync(
        Guid userId,
        bool consumedFromDailyFree,
        string originalIdempotencyKey,
        string reason = "refund_gemini_error",
        CancellationToken cancellationToken = default);

    Task<AiQuotaGrantResult> GrantBonusCreditsAsync(
        Guid userId,
        int amount,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AiQuotaStatusDto> GetGuestQuotaAsync(
        string clientIp,
        CancellationToken cancellationToken = default);

    Task<AiQuotaConsumeResult> TryConsumeGuestTurnAsync(
        string clientIp,
        string reason,
        CancellationToken cancellationToken = default);

    Task<AiQuotaStatusDto> RefundGuestTurnAsync(
        string clientIp,
        CancellationToken cancellationToken = default);
}
