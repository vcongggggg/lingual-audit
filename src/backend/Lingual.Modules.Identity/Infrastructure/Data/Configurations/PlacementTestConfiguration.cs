using Lingual.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Identity.Infrastructure.Data.Configurations;

public class PlacementTestConfiguration : IEntityTypeConfiguration<PlacementTest>
{
    public void Configure(EntityTypeBuilder<PlacementTest> builder)
    {
        builder.ToTable("placement_tests");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasMaxLength(16)
            .HasDefaultValue("in_progress")
            .IsRequired();

        builder.Property(p => p.QuestionCount)
            .HasDefaultValue((short)10)
            .IsRequired();

        builder.Property(p => p.CorrectCount);

        builder.Property(p => p.AssessedLevel)
            .HasMaxLength(2);

        builder.Property(p => p.AnswersDetail)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(p => p.StartedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.CompletedAt);

        builder.HasIndex(p => new { p.UserId, p.StartedAt });
    }
}
