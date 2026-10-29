using Lingual.Modules.MezonBot.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class QuizConfiguration : BaseConfiguration<Quiz>
{
    public override void Configure(EntityTypeBuilder<Quiz> builder)
    {
        base.Configure(builder);

        builder.ToTable("quizzes");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(q => q.Code)
            .HasMaxLength(80)
            .IsRequired();
        builder.HasIndex(q => q.Code)
            .IsUnique();

        builder.Property(q => q.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(q => q.Description);

        builder.Property(q => q.QuizType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.Level)
            .HasMaxLength(2);

        builder.Property(q => q.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(q => q.TimeLimitSeconds);

        builder.Property(q => q.TotalQuestions)
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(q => q.CreatedBy);

        builder.HasMany(q => q.Questions)
            .WithOne(qq => qq.Quiz)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Attempts)
            .WithOne(qa => qa.Quiz)
            .HasForeignKey(qa => qa.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.QuizType, q.Level, q.Status });
    }
}
