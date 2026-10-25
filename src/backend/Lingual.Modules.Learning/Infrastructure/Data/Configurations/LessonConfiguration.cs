using Lingual.Modules.Learning.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class LessonConfiguration : BaseConfiguration<Lesson>
{
    public override void Configure(EntityTypeBuilder<Lesson> builder)
    {
        base.Configure(builder);

        builder.ToTable("lessons");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.UnitId)
            .IsRequired();

        builder.Property(l => l.Code)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(l => l.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Summary);

        builder.Property(l => l.EstimatedMinutes);

        builder.Property(l => l.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(l => l.VocabularyIds)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.HasIndex(l => new { l.UnitId, l.Code })
            .IsUnique();

        builder.HasIndex(l => new { l.UnitId, l.SortOrder })
            .IsUnique();
    }
}
