using Lingual.Modules.MezonBot.Domain;
using Lingual.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lingual.Modules.MezonBot.Infrastructure.Data;

public class MezonBotDbContext(DbContextOptions<MezonBotDbContext> options)
    : ApplicationDbContextBase(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<BotConfiguration> BotConfigurations => Set<BotConfiguration>();
    public DbSet<ClanQuizSession> ClanQuizSessions => Set<ClanQuizSession>();
    public DbSet<ClanModeratorGrant> ClanModeratorGrants => Set<ClanModeratorGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MezonBotDbContext).Assembly);
    }
}
