using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class XpService(
    GamificationDbContext dbContext,
    IStreakService streakService,
    ILeaderboardService leaderboardService,
    ILogger<XpService> logger) : IXpService
{
    public async Task<AwardXpResult> AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return new AwardXpResult(
                IsSuccess: false,
                AwardedXp: 0,
                TotalXp: 0,
                CurrentStreak: 0,
                IsStreakActiveToday: false,
                AlreadyAwarded: false,
                Message: "UserId không hợp lệ.");
        }

        if (request.XpDelta <= 0)
        {
            var currentStreak = await streakService.GetStreakAsync(request.UserId, cancellationToken);
            var currentTotal = await GetTotalXpAsync(request.UserId, cancellationToken);
            return new AwardXpResult(
                IsSuccess: true,
                AwardedXp: 0,
                TotalXp: currentTotal,
                CurrentStreak: currentStreak.CurrentDays,
                IsStreakActiveToday: currentStreak.IsActiveToday,
                AlreadyAwarded: false,
                Message: "XpDelta phải lớn hơn 0.");
        }

        // 1. Kiểm tra Idempotency chống cộng trùng
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            bool exists = await dbContext.XpLedgers.AnyAsync(
                x => x.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation("Phát hiện request cộng XP trùng với idempotencyKey: {Key}", request.IdempotencyKey);
                var existingStreak = await streakService.GetStreakAsync(request.UserId, cancellationToken);
                var existingTotal = await GetTotalXpAsync(request.UserId, cancellationToken);

                return new AwardXpResult(
                    IsSuccess: true,
                    AwardedXp: 0,
                    TotalXp: existingTotal,
                    CurrentStreak: existingStreak.CurrentDays,
                    IsStreakActiveToday: existingStreak.IsActiveToday,
                    AlreadyAwarded: true,
                    Message: "Yêu cầu đã được ghi nhận trước đó (Idempotent).");
            }
        }
        else if (request.SourceId.HasValue && !string.IsNullOrWhiteSpace(request.SourceType))
        {
            bool exists = await dbContext.XpLedgers.AnyAsync(
                x => x.SourceType == request.SourceType && x.SourceId == request.SourceId,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation("Phát hiện request cộng XP trùng cho source {Type}:{Id}", request.SourceType, request.SourceId);
                var existingStreak = await streakService.GetStreakAsync(request.UserId, cancellationToken);
                var existingTotal = await GetTotalXpAsync(request.UserId, cancellationToken);

                return new AwardXpResult(
                    IsSuccess: true,
                    AwardedXp: 0,
                    TotalXp: existingTotal,
                    CurrentStreak: existingStreak.CurrentDays,
                    IsStreakActiveToday: existingStreak.IsActiveToday,
                    AlreadyAwarded: true,
                    Message: "Nguồn thưởng này đã được trao trước đó.");
            }
        }

        // 2. Ghi nhận giao dịch vào sổ cái bất biến (xp_ledger)
        var ledger = new XpLedger
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            XpDelta = request.XpDelta,
            AwardedAt = DateTime.UtcNow,
            IdempotencyKey = request.IdempotencyKey,
            Metadata = request.Metadata ?? "{}"
        };

        dbContext.XpLedgers.Add(ledger);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Tăng điểm bảng xếp hạng trên Redis (ZINCRBY)
        await leaderboardService.IncrementXpAsync(request.UserId.ToString(), request.XpDelta, cancellationToken);

        // 4. Cập nhật chuỗi Streak (kiểm tra mốc 20 XP/ngày theo giờ VN)
        var updatedStreak = await streakService.RecordXpActivityAsync(
            request.UserId,
            request.XpDelta,
            cancellationToken: cancellationToken);

        // 5. Tính tổng XP
        int totalXp = await GetTotalXpAsync(request.UserId, cancellationToken);

        return new AwardXpResult(
            IsSuccess: true,
            AwardedXp: request.XpDelta,
            TotalXp: totalXp,
            CurrentStreak: updatedStreak.CurrentDays,
            IsStreakActiveToday: updatedStreak.IsActiveToday,
            AlreadyAwarded: false);
    }

    public async Task<int> GetTotalXpAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.XpLedgers
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.XpDelta, cancellationToken);
    }
}

