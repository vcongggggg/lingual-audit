using Lingual.Modules.AITutor.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.AITutor.Infrastructure.Data.Configurations;

public class AiScenarioConfiguration : BaseConfiguration<AiScenario>
{
    public override void Configure(EntityTypeBuilder<AiScenario> builder)
    {
        base.Configure(builder);

        builder.ToTable("ai_scenarios");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.Code)
            .HasMaxLength(80)
            .IsRequired();
        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(s => s.Description);

        builder.Property(s => s.Level)
            .HasMaxLength(2);

        builder.Property(s => s.SystemPromptKey)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(s => s.CreatedBy);

        builder.HasMany(s => s.Conversations)
            .WithOne(c => c.Scenario)
            .HasForeignKey(c => c.ScenarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.Level, s.Status });
    }
}
