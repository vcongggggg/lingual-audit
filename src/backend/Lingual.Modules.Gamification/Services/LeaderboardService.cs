using Lingual.Infrastructure.Cache.Redis;
using Lingual.Modules.Gamification.Common;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class LeaderboardService(
    IRedisService redisService,
    GamificationDbContext dbContext,
    ILogger<LeaderboardService> logger) : ILeaderboardService
{
    private const string AllTimeLeaderboardKey = "leaderboard:all-time";
    private const string DemoUserKey = "lingual-local-demo-user";

    public async Task IncrementXpAsync(string userId, double xpDelta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || xpDelta <= 0)
        {
            return;
        }

        try
        {
            var weeklyKey = GetWeeklyKey();

            // Tăng điểm trên Redis Sorted Set (ZINCRBY)
            await redisService.SortedSetIncrementAsync(AllTimeLeaderboardKey, userId, xpDelta);
            await redisService.SortedSetIncrementAsync(weeklyKey, userId, xpDelta);

            // TTL 30 ngày cho key tuần
            await redisService.KeyExpireAsync(weeklyKey, TimeSpan.FromDays(30));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi cập nhật bảng xếp hạng Redis cho user {UserId}", userId);
        }
    }

    public async Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var key = type.Equals("weekly", StringComparison.OrdinalIgnoreCase)
            ? GetWeeklyKey()
            : AllTimeLeaderboardKey;

        List<(string Member, double Score)> entries;

        try
        {
            entries = (await redisService.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1, descending: true)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể đọc Redis Leaderboard, chuyển sang truy vấn trực tiếp DB");
            entries = [];
        }

        // Nếu Redis chưa có dữ liệu, hydrate từ DB
        if (entries.Count == 0)
        {
            entries = await HydrateLeaderboardFromDbAsync(key, limit, cancellationToken);
        }

        var resultEntries = new List<LeaderboardEntryDto>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            var (member, score) = entries[i];
            string displayName = member == DemoUserKey ? "You" : member;

            resultEntries.Add(new LeaderboardEntryDto(
                Rank: i + 1,
                UserId: member,
                Name: displayName,
                Xp: score));
        }

        LeaderboardEntryDto? myRank = null;
        if (currentUserId.HasValue)
        {
            var userIdStr = currentUserId.Value.ToString();
            try
            {
                var rankZeroBased = await redisService.SortedSetRankAsync(key, userIdStr, descending: true);
                var score = await redisService.SortedSetScoreAsync(key, userIdStr);

                if (rankZeroBased.HasValue && score.HasValue)
                {
                    myRank = new LeaderboardEntryDto(
                        Rank: (int)rankZeroBased.Value + 1,
                        UserId: userIdStr,
                        Name: "You",
                        Xp: score.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi lấy MyRank từ Redis cho user {UserId}", currentUserId.Value);
            }
        }

        return new LeaderboardResponseDto(
            UpdatedAt: DateTimeOffset.UtcNow,
            Entries: resultEntries,
            MyRank: myRank,
            TotalParticipants: resultEntries.Count);
    }

    public async Task FinalizeWeeklyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var (weekStart, weekEnd) = TimeZoneHelper.GetCurrentWeekRange();
        var weeklyKey = GetWeeklyKey();

        try
        {
            var entries = (await redisService.SortedSetRangeByRankWithScoresAsync(weeklyKey, 0, -1, descending: true)).ToList();

            for (short i = 0; i < entries.Count; i++)
            {
                var (member, score) = entries[i];
                if (!Guid.TryParse(member, out var userId))
                {
                    continue;
                }

                var existing = await dbContext.WeeklyLeaderboards
                    .FirstOrDefaultAsync(w => w.WeekStart == weekStart && w.UserId == userId, cancellationToken);

                if (existing == null)
                {
                    existing = new WeeklyLeaderboard
                    {
                        WeekStart = weekStart,
                        WeekEnd = weekEnd,
                        UserId = userId,
                        XpTotal = (int)score,
                        Rank = (short)(i + 1),
                        Status = "finalized",
                        FinalizedAt = DateTime.UtcNow
                    };
                    dbContext.WeeklyLeaderboards.Add(existing);
                }
                else
                {
                    existing.XpTotal = (int)score;
                    existing.Rank = (short)(i + 1);
                    existing.Status = "finalized";
                    existing.FinalizedAt = DateTime.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã chốt bảng xếp hạng tuần {WeekStart} thành công với {Count} học viên", weekStart, entries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi chốt bảng xếp hạng tuần vào DB");
        }
    }

    private static string GetWeeklyKey() => $"leaderboard:weekly:{TimeZoneHelper.GetCurrentWeekKey()}";

    private async Task<List<(string Member, double Score)>> HydrateLeaderboardFromDbAsync(
        string key,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var seedData = await dbContext.XpLedgers
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key.ToString(), TotalXp = (double)g.Sum(x => x.XpDelta) })
                .OrderByDescending(x => x.TotalXp)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var result = new List<(string Member, double Score)>();
            foreach (var item in seedData)
            {
                result.Add((item.UserId, item.TotalXp));
                await redisService.SortedSetAddAsync(key, item.UserId, item.TotalXp);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi hydrate Leaderboard từ DB");
            return [];
        }
    }
}

