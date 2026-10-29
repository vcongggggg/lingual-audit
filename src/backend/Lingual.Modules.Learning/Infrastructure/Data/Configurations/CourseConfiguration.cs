using Lingual.Modules.Learning.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class CourseConfiguration : BaseConfiguration<Course>
{
    public override void Configure(EntityTypeBuilder<Course> builder)
    {
        base.Configure(builder);

        builder.ToTable("courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Code)
            .HasMaxLength(80)
            .IsRequired();
        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description);

        builder.Property(c => c.SourceLanguage)
            .HasMaxLength(16)
            .HasDefaultValue("en")
            .IsRequired();

        builder.Property(c => c.TargetLanguage)
            .HasMaxLength(16)
            .HasDefaultValue("vi")
            .IsRequired();

        builder.Property(c => c.Level)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.CreatedBy);

        builder.HasMany(c => c.Units)
            .WithOne(u => u.Course)
            .HasForeignKey(u => u.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
