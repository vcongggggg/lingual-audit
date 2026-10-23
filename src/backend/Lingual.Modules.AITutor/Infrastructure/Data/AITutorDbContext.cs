using Lingual.Modules.AITutor.Domain;
using Lingual.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lingual.Modules.AITutor.Infrastructure.Data;

public class AITutorDbContext(DbContextOptions<AITutorDbContext> options)
    : ApplicationDbContextBase(options)
{
    public DbSet<AiScenario> AiScenarios => Set<AiScenario>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AITutorDbContext).Assembly);
    }
}
