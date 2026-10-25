using Lingual.Modules.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("lesson_progress");

        builder.HasKey(lp => new { lp.UserId, lp.LessonId });

        builder.Property(lp => lp.UserId)
            .IsRequired();

        builder.Property(lp => lp.LessonId)
            .IsRequired();

        builder.Property(lp => lp.Status)
            .HasMaxLength(16)
            .HasDefaultValue("not_started")
            .IsRequired();

        builder.Property(lp => lp.FirstStartedAt);

        builder.Property(lp => lp.CompletedAt);

        builder.Property(lp => lp.BestScore)
            .HasPrecision(5, 2);

        builder.Property(lp => lp.SessionHistory)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.HasOne(lp => lp.Lesson)
            .WithMany(l => l.Progresses)
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
