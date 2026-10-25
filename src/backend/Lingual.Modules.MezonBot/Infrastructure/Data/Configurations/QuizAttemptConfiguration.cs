using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("quiz_attempts");

        builder.HasKey(qa => qa.Id);
        builder.Property(qa => qa.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(qa => qa.QuizId)
            .IsRequired();

        builder.Property(qa => qa.UserId)
            .IsRequired();

        builder.Property(qa => qa.Status)
            .HasMaxLength(16)
            .HasDefaultValue("in_progress")
            .IsRequired();

        builder.Property(qa => qa.Score)
            .HasPrecision(7, 2)
            .HasDefaultValue(0m);

        builder.Property(qa => qa.MaxScore)
            .HasPrecision(7, 2)
            .HasDefaultValue(0m);

        builder.Property(qa => qa.CorrectCount)
            .HasDefaultValue((short)0);

        builder.Property(qa => qa.AnswersDetail)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(qa => qa.StartedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(qa => qa.SubmittedAt);

        builder.HasIndex(qa => new { qa.UserId, qa.StartedAt });
        builder.HasIndex(qa => new { qa.QuizId, qa.Status });
    }
}
