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
        var userBadges = await dbContext.UserBadges
            .AsTracking()
            .Where(ub => ub.UserId == userId)
            .ToListAsync(cancellationToken);
        var userBadgeMap = userBadges.ToDictionary(ub => ub.BadgeId);

        // 3. Đọc dữ liệu chuỗi Streak và tổng XP từ CSDL để tính toán đồng bộ
        var streak = await dbContext.UserStreaks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var totalXp = await dbContext.XpLedgers.AsNoTracking()
            .Where(x => x.UserId == userId)
            .SumAsync(x => (int?)x.XpDelta, cancellationToken) ?? 0;

        int currentStreak = streak?.CurrentDays ?? 0;
        int bestStreak = Math.Max(currentStreak, streak?.BestDays ?? 0);

        int quizzesCompleted = await dbContext.XpLedgers.AsNoTracking()
            .CountAsync(x => x.UserId == userId &&
                (x.SourceType == "quiz" ||
                 x.SourceType == "quiz_attempt" ||
                 x.SourceType == "clan_quiz" ||
                 x.SourceType.StartsWith("game_")), cancellationToken);

        int gameWins = await dbContext.GameSessions.AsNoTracking()
            .CountAsync(g => g.UserId == userId && g.Score > 0, cancellationToken);

        int wordsLearned = 0;
        if (dbContext.Database.IsRelational())
        {
            try
            {
                var conn = dbContext.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM srs_cards WHERE user_id = @userId AND suspended_at IS NULL";
                var p = cmd.CreateParameter();
                p.ParameterName = "@userId";
                p.Value = userId;
                cmd.Parameters.Add(p);
                var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                if (scalar != null && scalar != DBNull.Value)
                    wordsLearned = Convert.ToInt32(scalar);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not count srs_cards for badge evaluation of user {UserId}", userId);
            }
        }

        var resultList = new List<BadgeDto>();
        bool hasChanges = false;

        foreach (var badge in badges)
        {
            userBadgeMap.TryGetValue(badge.Id, out var ub);

            bool isUnlocked = ub?.IsUnlocked ?? false;
            int progress = ub?.CurrentProgress ?? 0;
            string? unlockedDate = ub?.UnlockedAt?.ToString("dd/MM/yyyy");

            switch (badge.Id)
            {
                case "b-1": // Kỷ Lục Gia Streak
                    progress = Math.Max(progress, bestStreak);
                    break;

                case "b-2": // Kho Báu Từ Vựng (A1-A2)
                    progress = Math.Max(progress, wordsLearned);
                    break;

                case "b-3": // Bất Khả Chiến Bại
                    progress = Math.Max(progress, quizzesCompleted);
                    break;

                case "b-4": // Chiến Hữu Mezon (XP tích lũy)
                    progress = Math.Max(progress, totalXp);
                    break;

                case "b-5": // Đại Sứ Học Thuật
                    progress = Math.Max(progress, wordsLearned);
                    break;

                case "b-6": // Đấu Vương Game Center / Thời Gian Thực
                    progress = Math.Max(progress, gameWins);
                    break;
            }

            if (progress >= badge.MaxProgress && badge.MaxProgress > 0)
            {
                isUnlocked = true;
                unlockedDate ??= DateTime.UtcNow.ToString("dd/MM/yyyy");
            }

            // Đồng bộ trạng thái vào DB nếu có cập nhật mới
            if (ub == null)
            {
                var newUb = new Domain.UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    IsUnlocked = isUnlocked,
                    CurrentProgress = progress,
                    UnlockedAt = isUnlocked ? DateTime.UtcNow : null,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.UserBadges.Add(newUb);
                hasChanges = true;
            }
            else if (progress > ub.CurrentProgress || (!ub.IsUnlocked && isUnlocked))
            {
                ub.CurrentProgress = progress;
                if (!ub.IsUnlocked && isUnlocked)
                {
                    ub.IsUnlocked = true;
                    ub.UnlockedAt = DateTime.UtcNow;
                }
                ub.UpdatedAt = DateTime.UtcNow;
                hasChanges = true;
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

        if (hasChanges)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Đã lưu tiến độ huy hiệu vào user_badges cho user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi lưu bảng user_badges cho user {UserId}", userId);
            }
        }

        return new BadgesResponseDto(resultList);
    }
}
