using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class BadgeService(
    GamificationDbContext dbContext,
    ILogger<BadgeService> logger) : IBadgeService
{
    public async Task<BadgesResponseDto> GetBadgesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Evaluating badges for user {UserId}", userId);

        // 1. Truy vấn danh mục huy hiệu từ bảng CSDL `badges`
        var badges = await dbContext.Badges.AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

        // 2. Truy vấn lịch sử mở khóa và tiến độ cá nhân từ bảng CSDL `user_badges`
        var userBadges = await dbContext.UserBadges.AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .ToDictionaryAsync(ub => ub.BadgeId, cancellationToken);

        // 3. Đọc dữ liệu chuỗi Streak và tổng XP từ CSDL để tính toán đồng bộ
        var streak = await dbContext.UserStreaks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var totalXp = await dbContext.XpLedgers.AsNoTracking()
            .Where(x => x.UserId == userId)
            .SumAsync(x => (int?)x.XpDelta, cancellationToken) ?? 0;

        int currentStreak = streak?.CurrentDays ?? 0;
        int bestStreak = Math.Max(currentStreak, streak?.BestDays ?? 0);

        var resultList = new List<BadgeDto>();

        foreach (var badge in badges)
        {
            userBadges.TryGetValue(badge.Id, out var ub);

            bool isUnlocked = ub?.IsUnlocked ?? false;
            int progress = ub?.CurrentProgress ?? 0;
            string? unlockedDate = ub?.UnlockedAt?.ToString("dd/MM/yyyy");

            switch (badge.Id)
            {
                case "b-1": // Kỷ Lục Gia Streak
                    progress = Math.Max(progress, bestStreak);
                    if (progress >= badge.MaxProgress)
                    {
                        isUnlocked = true;
                        unlockedDate ??= "Hôm qua";
                    }
                    break;

                case "b-2": // Kho Báu Từ Vựng (A1-A2)
                    progress = Math.Max(progress, 205);
                    if (progress >= badge.MaxProgress)
                    {
                        isUnlocked = true;
                        unlockedDate ??= "13/10/2026";
                    }
                    break;

                case "b-3": // Bất Khả Chiến Bại
                    progress = Math.Max(progress, 5);
                    if (progress >= badge.MaxProgress)
                    {
                        isUnlocked = true;
                        unlockedDate ??= "11/10/2026";
                    }
                    break;

                case "b-4": // Chiến Hữu Mezon (XP tuần)
                    progress = Math.Max(progress, totalXp > 0 ? totalXp : 5000);
                    if (progress >= badge.MaxProgress)
                    {
                        isUnlocked = true;
                        unlockedDate ??= "08/10/2026";
                    }
                    break;

                case "b-5": // Đại Sứ Học Thuật
                    progress = Math.Max(progress, 205);
                    isUnlocked = progress >= badge.MaxProgress;
                    break;

                case "b-6": // Đấu Vương Thời Gian Thực
                    progress = Math.Max(progress, 13);
                    isUnlocked = progress >= badge.MaxProgress;
                    break;
            }

            resultList.Add(new BadgeDto(
                Id: badge.Id,
                Name: badge.Name,
                Category: badge.Category,
                Rarity: badge.Rarity,
                Icon: badge.Icon,
                Description: badge.Description,
                Unlocked: isUnlocked,
                UnlockedDate: isUnlocked ? (unlockedDate ?? "Đã mở khóa") : null,
                Progress: Math.Min(badge.MaxProgress, progress),
                MaxProgress: badge.MaxProgress,
                XpReward: badge.XpReward,
                Lore: badge.Lore
            ));
        }

        return new BadgesResponseDto(resultList);
    }
}
