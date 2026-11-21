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
    private const string AllTimeClanLeaderboardKey = "leaderboard:clan:all-time";
    private const string DemoUserKey = "lingual-local-demo-user";

    public static readonly HashSet<string> ExcludedUserIds = new(StringComparer.OrdinalIgnoreCase)
    {
        DemoUserKey,
        "00000000-0000-0000-0000-000000000001",
        "4943737a-792a-4fca-8315-df58d25c2156"
    };

    public static bool IsExcludedUser(string? userId) =>
        string.IsNullOrWhiteSpace(userId) || ExcludedUserIds.Contains(userId);

    private record ClanMeta(string Id, string Name, string Tag, string Avatar, int MemberCount, double BaseXp);

    private static readonly IReadOnlyList<ClanMeta> DefaultClans =
    [
        new("clan-1", "BK Lexicon Legion", "BKLX", "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=150&auto=format&fit=crop&q=80", 30, 62400),
        new("clan-2", "LinguaMezon Squad", "MEZON", "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150&auto=format&fit=crop&q=80", 28, 54900),
        new("clan-3", "IELTS 8.0 Conquerors", "IELTS", "https://images.unsplash.com/photo-1614850523459-c2f4c699c52e?w=150&auto=format&fit=crop&q=80", 25, 50100),
        new("clan-4", "NCC Polyglots", "NCC", "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=150&auto=format&fit=crop&q=80", 22, 44200),
        new("clan-5", "Flashcard Warriors", "WAR", "https://images.unsplash.com/photo-1563089145-599997674d42?w=150&auto=format&fit=crop&q=80", 20, 38600)
    ];

    public async Task IncrementXpAsync(string userId, double xpDelta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || xpDelta <= 0 || IsExcludedUser(userId))
        {
            return;
        }

        try
        {
            var weeklyKey = GetWeeklyKey();
            var monthlyKey = GetMonthlyKey();

            // Tăng điểm trên Redis Sorted Set (ZINCRBY)
            await redisService.SortedSetIncrementAsync(AllTimeLeaderboardKey, userId, xpDelta);
            await redisService.SortedSetIncrementAsync(weeklyKey, userId, xpDelta);
            await redisService.SortedSetIncrementAsync(monthlyKey, userId, xpDelta);

            // TTL cho key tuần và tháng
            await redisService.KeyExpireAsync(weeklyKey, TimeSpan.FromDays(30));
            await redisService.KeyExpireAsync(monthlyKey, TimeSpan.FromDays(60));
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
        string normalizedType = type.ToLowerInvariant().Replace("_", "-");
        var key = normalizedType switch
        {
            "weekly" or "week" => GetWeeklyKey(),
            "monthly" or "month" => GetMonthlyKey(),
            _ => AllTimeLeaderboardKey
        };

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
            entries = await HydrateLeaderboardFromDbAsync(key, normalizedType, limit, cancellationToken);
        }

        // Lấy thông tin streak của các user nếu có
        var userGuids = entries
            .Select(e => Guid.TryParse(e.Member, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        var streakMap = new Dictionary<Guid, int>();
        var userProfiles = new Dictionary<Guid, (string DisplayName, string? AvatarUrl)>();
        if (userGuids.Count > 0)
        {
            try
            {
                streakMap = await dbContext.UserStreaks.AsNoTracking()
                    .Where(s => userGuids.Contains(s.UserId))
                    .ToDictionaryAsync(s => s.UserId, s => s.CurrentDays, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không thể đọc UserStreaks cho leaderboard");
            }

            try
            {
                var conn = dbContext.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var cmd = conn.CreateCommand();
                var idList = string.Join(",", userGuids.Select(g => $"'{g}'"));
                cmd.CommandText = $"SELECT id, display_name, avatar_url FROM users WHERE id IN ({idList})";

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var avatar = reader.IsDBNull(2) ? null : reader.GetString(2);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        userProfiles[id] = (name, avatar);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không thể đọc thông tin người dùng từ bảng users cho leaderboard");
            }
        }

        var resultEntries = new List<LeaderboardEntryDto>(entries.Count);
        int currentRank = 1;
        for (int i = 0; i < entries.Count; i++)
        {
            var (member, score) = entries[i];
            if (IsExcludedUser(member))
            {
                _ = redisService.SortedSetRemoveAsync(key, member);
                continue;
            }

            string displayName = member;
            string? avatarUrl = null;
            int streakDays = 0;
            if (Guid.TryParse(member, out var g))
            {
                if (userProfiles.TryGetValue(g, out var profile))
                {
                    displayName = profile.DisplayName;
                    avatarUrl = profile.AvatarUrl;
                }
                if (streakMap.TryGetValue(g, out var days))
                {
                    streakDays = days;
                }
            }

            resultEntries.Add(new LeaderboardEntryDto(
                Rank: currentRank,
                UserId: member,
                Name: displayName,
                Xp: score,
                AvatarUrl: avatarUrl,
                Level: Math.Max(1, (int)(score / 500) + 1),
                WordsLearned: (int)(score / 15),
                StreakDays: streakDays,
                PreviousRank: currentRank));
            currentRank++;
        }

        LeaderboardEntryDto? myRank = null;
        if (currentUserId.HasValue && !IsExcludedUser(currentUserId.Value.ToString()))
        {
            var userIdStr = currentUserId.Value.ToString();
            try
            {
                var rankZeroBased = await redisService.SortedSetRankAsync(key, userIdStr, descending: true);
                var score = await redisService.SortedSetScoreAsync(key, userIdStr);

                if (rankZeroBased.HasValue && score.HasValue)
                {
                    streakMap.TryGetValue(currentUserId.Value, out var myStreak);
                    userProfiles.TryGetValue(currentUserId.Value, out var myProfile);
                    myRank = new LeaderboardEntryDto(
                        Rank: (int)rankZeroBased.Value + 1,
                        UserId: userIdStr,
                        Name: myProfile.DisplayName ?? "You",
                        Xp: score.Value,
                        AvatarUrl: myProfile.AvatarUrl,
                        Level: Math.Max(1, (int)(score.Value / 500) + 1),
                        WordsLearned: (int)(score.Value / 15),
                        StreakDays: myStreak,
                        PreviousRank: (int)rankZeroBased.Value + 1);
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

    public async Task<ClanLeaderboardResponseDto> GetClanLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        string normalizedType = type.ToLowerInvariant().Replace("_", "-");
        var key = normalizedType switch
        {
            "weekly" or "week" => $"leaderboard:clan:weekly:{TimeZoneHelper.GetCurrentWeekKey()}",
            "monthly" or "month" => $"leaderboard:clan:monthly:{TimeZoneHelper.GetCurrentMonthKey()}",
            _ => AllTimeClanLeaderboardKey
        };

        List<(string Member, double Score)> entries;
        try
        {
            entries = (await redisService.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1, descending: true)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể đọc Redis Clan Leaderboard");
            entries = [];
        }

        // Hydrate default clans nếu Redis chưa có
        if (entries.Count == 0)
        {
            double scale = normalizedType switch
            {
                "weekly" or "week" => 0.25,
                "monthly" or "month" => 0.65,
                _ => 1.0
            };

            entries = [];
            foreach (var clan in DefaultClans)
            {
                double score = Math.Round(clan.BaseXp * scale);
                entries.Add((clan.Id, score));
                try
                {
                    await redisService.SortedSetAddAsync(key, clan.Id, score);
                }
                catch
                {
                    // Ignore cache write error
                }
            }
            entries = entries.OrderByDescending(e => e.Score).Take(limit).ToList();
        }

        var metaMap = DefaultClans.ToDictionary(c => c.Id);
        var clanEntries = new List<ClanLeaderboardEntryDto>(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            var (clanId, score) = entries[i];
            metaMap.TryGetValue(clanId, out var meta);

            string name = meta?.Name ?? $"Clan {clanId}";
            string tag = meta?.Tag ?? "CLAN";
            string avatar = meta?.Avatar ?? "";
            int memberCount = meta?.MemberCount ?? 20;
            int level = Math.Max(1, (int)(score / 1500) + 1);
            int wordsLearned = (int)(score * 0.08);
            bool isCurrent = clanId == "clan-2";

            clanEntries.Add(new ClanLeaderboardEntryDto(
                Id: clanId,
                Rank: i + 1,
                PreviousRank: i + 1,
                Name: name,
                Avatar: avatar,
                Level: level,
                Tag: tag,
                WordsLearned: wordsLearned,
                Xp: score,
                MemberCount: memberCount,
                IsCurrentUser: isCurrent));
        }

        return new ClanLeaderboardResponseDto(
            UpdatedAt: DateTimeOffset.UtcNow,
            Entries: clanEntries,
            TotalClans: clanEntries.Count);
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
                if (!Guid.TryParse(member, out var userId) || IsExcludedUser(member))
                {
                    continue;
                }

                var existing = await dbContext.WeeklyLeaderboards
                    .AsTracking()
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
    private static string GetMonthlyKey() => $"leaderboard:monthly:{TimeZoneHelper.GetCurrentMonthKey()}";

    private async Task<List<(string Member, double Score)>> HydrateLeaderboardFromDbAsync(
        string key,
        string type,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var excludedGuids = new HashSet<Guid>
            {
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("4943737a-792a-4fca-8315-df58d25c2156")
            };

            var query = dbContext.XpLedgers.AsNoTracking()
                .Where(x => !excludedGuids.Contains(x.UserId));

            if (type is "weekly" or "week")
            {
                var (weekStart, _) = TimeZoneHelper.GetCurrentWeekRange();
                var fromUtc = weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(x => x.AwardedAt >= fromUtc);
            }
            else if (type is "monthly" or "month")
            {
                var (monthStart, _) = TimeZoneHelper.GetCurrentMonthRange();
                var fromUtc = monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(x => x.AwardedAt >= fromUtc);
            }

            var seedData = await query
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key.ToString(), TotalXp = (double)g.Sum(x => x.XpDelta) })
                .OrderByDescending(x => x.TotalXp)
                .Take(limit)
                .ToListAsync(cancellationToken);

            // Nếu khung tuần/tháng chưa có dữ liệu mới phát sinh, hydrate từ all-time với hệ số tỉ lệ
            if (seedData.Count == 0 && type is not "all-time" and not "alltime")
            {
                double scale = type is "weekly" or "week" ? 0.3 : 0.7;
                var allTimeData = await dbContext.XpLedgers.AsNoTracking()
                    .Where(x => !excludedGuids.Contains(x.UserId))
                    .GroupBy(x => x.UserId)
                    .Select(g => new { UserId = g.Key.ToString(), TotalXp = (double)g.Sum(x => x.XpDelta) })
                    .OrderByDescending(x => x.TotalXp)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                seedData = allTimeData.Select(x => new { x.UserId, TotalXp = Math.Round(x.TotalXp * scale) }).ToList();
            }

            var result = new List<(string Member, double Score)>();
            foreach (var item in seedData)
            {
                result.Add((item.UserId, item.TotalXp));
                try
                {
                    await redisService.SortedSetAddAsync(key, item.UserId, item.TotalXp);
                }
                catch
                {
                    // Redis offline: still return hydrated PostgreSQL entries immediately
                }
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

