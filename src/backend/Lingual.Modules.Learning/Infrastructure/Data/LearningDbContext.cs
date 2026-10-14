using Lingual.Modules.Learning.Domain;
using Lingual.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lingual.Modules.Learning.Infrastructure.Data;

public class LearningDbContext(DbContextOptions<LearningDbContext> options)
    : ApplicationDbContextBase(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<SrsCard> SrsCards => Set<SrsCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
