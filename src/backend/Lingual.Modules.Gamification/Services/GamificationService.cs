using System.Text.Json;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Domain.Models;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Services;

public class GamificationService(
    IXpService xpService,
    IStreakService streakService,
    ILeaderboardService leaderboardService,
    IBadgeService badgeService,
    GamificationDbContext dbContext,
    ILogger<GamificationService> logger) : IGamificationService
{
    private static readonly Guid DefaultDemoUserId = Guid.Parse("4943737a-792a-4fca-8315-df58d25c2156");

    public Task<AwardXpResult> AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken = default)
    {
        return xpService.AwardXpAsync(request, cancellationToken);
    }

    public Task<UserStreakDto> GetUserStreakAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.GetStreakAsync(userId, cancellationToken);
    }

    public Task<DailyCheckinResultDto> CheckInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.CheckInAsync(userId, cancellationToken);
    }

    public Task<StreakFreezeResultDto> UseStreakFreezeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return streakService.UseStreakFreezeAsync(userId, cancellationToken);
    }

    public Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        return leaderboardService.GetLeaderboardAsync(limit, type, currentUserId, cancellationToken);
    }

    public Task<ClanLeaderboardResponseDto> GetClanLeaderboardAsync(
        int limit = 20,
        string type = "all-time",
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        return leaderboardService.GetClanLeaderboardAsync(limit, type, currentUserId, cancellationToken);
    }

    public async Task<UserProfileSummaryDto> GetUserProfileSummaryAsync(Guid? userId, CancellationToken ct = default)
    {
        bool isDemoUser = !userId.HasValue || userId.Value == Guid.Empty || userId.Value == DefaultDemoUserId;
        var targetUserId = isDemoUser ? DefaultDemoUserId : userId!.Value;

        // 1. Fetch Streak
        UserStreakSummaryDto streakSummary;
        try
        {
            var streakDto = await streakService.GetStreakAsync(targetUserId, ct);
            streakSummary = new UserStreakSummaryDto(
                CurrentDays: isDemoUser && streakDto.CurrentDays == 0 ? 5 : streakDto.CurrentDays,
                BestDays: isDemoUser && streakDto.BestDays == 0 ? 12 : streakDto.BestDays,
                FreezeBalance: isDemoUser && streakDto.FreezeBalance <= 1 ? 2 : streakDto.FreezeBalance,
                IsActiveToday: isDemoUser ? true : streakDto.IsActiveToday,
                TodayXp: isDemoUser && streakDto.TodayXp == 0 ? 50 : streakDto.TodayXp,
                XpNeededForStreak: isDemoUser && streakDto.TodayXp == 0 ? 30 : streakDto.XpNeededForStreak);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get streak for user {UserId}", targetUserId);
            streakSummary = isDemoUser
                ? new UserStreakSummaryDto(5, 12, 2, true, 50, 30)
                : new UserStreakSummaryDto(0, 0, 1, false, 0, 20);
        }

        // 2. Fetch Badges
        List<BadgeDto> badgesList = [];
        try
        {
            var badgesResponse = await badgeService.GetBadgesAsync(targetUserId, ct);
            if (badgesResponse?.Badges != null)
            {
                badgesList.AddRange(badgesResponse.Badges);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get badges for user {UserId}", targetUserId);
        }

        if (isDemoUser && badgesList.All(b => b.Id != "streak_3"))
        {
            badgesList.Insert(0, new BadgeDto(
                Id: "streak_3",
                Name: "Chuỗi 3 ngày",
                Category: "streak",
                Rarity: "common",
                Icon: "flame",
                Description: "Duy trì chuỗi học tập 3 ngày liên tục",
                Unlocked: true,
                UnlockedDate: "2026-11-18",
                Progress: 3,
                MaxProgress: 3,
                XpReward: 50,
                Lore: "Khởi đầu ấn tượng!"
            ));
        }

        // 3. Fetch Total XP
        int totalXp = 0;
        try
        {
            totalXp = await xpService.GetTotalXpAsync(targetUserId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get total XP for user {UserId}", targetUserId);
        }

        if (isDemoUser && totalXp == 0)
        {
            totalXp = 450;
        }

        // 4. Sensible Level & Next Level XP calculations (200 XP per level)
        int level = Math.Max(1, (totalXp / 200) + 1);
        int nextLevelXp = level * 200;

        // 5. User Profile (Display Name, Avatar, Role)
        string displayName = isDemoUser ? "Nguyễn Văn A" : "Learner";
        string? avatarUrl = isDemoUser ? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80" : null;
        string role = "learner";

        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
            }

            using var cmd = conn.CreateCommand();
            var pUserId = cmd.CreateParameter();
            pUserId.ParameterName = "@userId";
            pUserId.Value = targetUserId;
            cmd.Parameters.Add(pUserId);
            cmd.CommandText = "SELECT display_name, avatar_url, role FROM users WHERE id = @userId LIMIT 1";

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                if (!reader.IsDBNull(0)) displayName = reader.GetString(0);
                if (!reader.IsDBNull(1)) avatarUrl = reader.GetString(1);
                if (!reader.IsDBNull(2)) role = reader.GetString(2);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch user details from users table for {UserId}", targetUserId);
        }

        // 6. User Performance Stats
        UserPerformanceStatsDto stats;
        if (isDemoUser)
        {
            stats = new UserPerformanceStatsDto(
                WordsLearned: 45,
                QuizzesCompleted: 12,
                AccuracyRate: 88);
        }
        else
        {
            int wordsLearned = 0;
            if (dbContext.Database.IsRelational())
            {
                try
                {
                    var conn = dbContext.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync(ct);

                    using var countCmd = conn.CreateCommand();
                    countCmd.CommandText = "SELECT COUNT(*) FROM srs_cards WHERE user_id = @userId AND suspended_at IS NULL";
                    var pUid = countCmd.CreateParameter();
                    pUid.ParameterName = "@userId";
                    pUid.Value = targetUserId;
                    countCmd.Parameters.Add(pUid);
                    var scalar = await countCmd.ExecuteScalarAsync(ct);
                    if (scalar != null && scalar != DBNull.Value)
                        wordsLearned = Convert.ToInt32(scalar);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not query srs_cards count for user {UserId}", targetUserId);
                }
            }

            if (wordsLearned == 0)
            {
                try
                {
                    var streakRecord = await dbContext.UserStreaks.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == targetUserId, ct);
                    if (!string.IsNullOrWhiteSpace(streakRecord?.DailyActivity))
                    {
                        var activities = JsonSerializer.Deserialize<Dictionary<string, DailyActivityEntry>>(streakRecord.DailyActivity);
                        if (activities != null)
                        {
                            wordsLearned = activities.Values.Sum(a => a.WordsCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to compute wordsLearned from DailyActivity for user {UserId}", targetUserId);
                }
            }

            int quizzesCompleted = 0;
            int quizzesThisWeek = 0;
            try
            {
                var sevenDaysAgoUtc = DateTime.UtcNow.Date.AddDays(-6);
                var quizEntries = await dbContext.XpLedgers.AsNoTracking()
                    .Where(x => x.UserId == targetUserId &&
                        (x.SourceType == "quiz" ||
                         x.SourceType == "quiz_attempt" ||
                         x.SourceType == "clan_quiz" ||
                         x.SourceType == "practice" ||
                         x.SourceType == "duel" ||
                         x.SourceType.StartsWith("game_")))
                    .Select(x => x.AwardedAt)
                    .ToListAsync(ct);
                quizzesCompleted = quizEntries.Count;
                quizzesThisWeek = quizEntries.Count(awardedAt => awardedAt >= sevenDaysAgoUtc);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to count quizzesCompleted for user {UserId}", targetUserId);
            }

            int accuracyRate = 0;
            try
            {
                var gameAccuracies = await dbContext.GameSessions.AsNoTracking()
                    .Where(g => g.UserId == targetUserId)
                    .Select(g => g.Accuracy)
                    .ToListAsync(ct);
                if (gameAccuracies.Count > 0)
                {
                    var avg = gameAccuracies.Average();
                    accuracyRate = (int)Math.Round(avg <= 1.0 ? avg * 100.0 : avg);
                }
                else if (quizzesCompleted > 0)
                {
                    // Fallback to quiz_attempt score/total ratio from xp_ledger metadata when available
                    var quizMetas = await dbContext.XpLedgers.AsNoTracking()
                        .Where(x => x.UserId == targetUserId && x.SourceType == "quiz_attempt")
                        .Select(x => x.Metadata)
                        .ToListAsync(ct);
                    int totalCorrect = 0, totalAsked = 0;
                    foreach (var meta in quizMetas)
                    {
                        if (string.IsNullOrWhiteSpace(meta)) continue;
                        try
                        {
                            using var doc = JsonDocument.Parse(meta);
                            if (doc.RootElement.TryGetProperty("score", out var sEl) &&
                                doc.RootElement.TryGetProperty("total", out var tEl))
                            {
                                totalCorrect += sEl.GetInt32();
                                totalAsked += tEl.GetInt32();
                            }
                        }
                        catch { }
                    }
                    if (totalAsked > 0)
                        accuracyRate = (int)Math.Round((double)totalCorrect * 100.0 / totalAsked);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to compute real accuracyRate for user {UserId}", targetUserId);
            }

            stats = new UserPerformanceStatsDto(
                WordsLearned: wordsLearned,
                QuizzesCompleted: quizzesCompleted,
                AccuracyRate: accuracyRate,
                QuizzesThisWeek: quizzesThisWeek);
        }

        return new UserProfileSummaryDto(
            UserId: targetUserId.ToString(),
            DisplayName: displayName,
            AvatarUrl: avatarUrl,
            Role: role,
            Level: level,
            TotalXp: totalXp,
            NextLevelXp: nextLevelXp,
            Streak: streakSummary,
            Badges: badgesList,
            Stats: stats);
    }
}
