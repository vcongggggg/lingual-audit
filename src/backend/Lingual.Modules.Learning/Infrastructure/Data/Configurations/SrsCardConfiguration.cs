using Lingual.Modules.Learning.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class SrsCardConfiguration : BaseConfiguration<SrsCard>
{
    public override void Configure(EntityTypeBuilder<SrsCard> builder)
    {
        base.Configure(builder);

        builder.ToTable("srs_cards");

        builder.HasKey(s => new { s.UserId, s.VocabularyId });

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.VocabularyId)
            .IsRequired();

        builder.Property(s => s.Repetitions)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.IntervalDays)
            .HasPrecision(8, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(s => s.EaseFactor)
            .HasPrecision(4, 2)
            .HasDefaultValue(2.50m)
            .IsRequired();

        builder.Property(s => s.NextDueAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.LastReviewedAt);

        builder.Property(s => s.Lapses)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.SuspendedAt);

        builder.Property(s => s.ReviewHistory)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.HasOne(s => s.VocabularyItem)
            .WithMany(v => v.SrsCards)
            .HasForeignKey(s => s.VocabularyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.NextDueAt });
    }
}
