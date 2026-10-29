using Lingual.Modules.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.CourseId)
            .IsRequired();

        builder.Property(u => u.Code)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(u => u.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Description);

        builder.Property(u => u.Topic)
            .HasMaxLength(120);

        builder.Property(u => u.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.HasIndex(u => new { u.CourseId, u.Code })
            .IsUnique();

        builder.HasIndex(u => new { u.CourseId, u.SortOrder })
            .IsUnique();

        builder.HasMany(u => u.Lessons)
            .WithOne(l => l.Unit)
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
