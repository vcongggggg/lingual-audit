using Lingual.Modules.Gamification.Domain;
using Lingual.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lingual.Modules.Gamification.Infrastructure.Data;

public class GamificationDbContext(DbContextOptions<GamificationDbContext> options)
    : ApplicationDbContextBase(options)
{
    public DbSet<XpLedger> XpLedgers => Set<XpLedger>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();
    public DbSet<DuelMatch> DuelMatches => Set<DuelMatch>();
    public DbSet<WeeklyLeaderboard> WeeklyLeaderboards => Set<WeeklyLeaderboard>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<UserGameStat> UserGameStats => Set<UserGameStat>();
    public DbSet<GameWordPool> GameWordPools => Set<GameWordPool>();
    public DbSet<GameQuestion> GameQuestions => Set<GameQuestion>();
    public DbSet<ActiveGameAttempt> ActiveGameAttempts => Set<ActiveGameAttempt>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamificationDbContext).Assembly);
    }
}
