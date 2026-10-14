using Lingual.Modules.AITutor.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.AITutor.Infrastructure.Data.Configurations;

public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("ai_conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ScenarioId);

        builder.Property(c => c.ConversationType)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(c => c.Title)
            .HasMaxLength(200);

        builder.Property(c => c.Status)
            .HasMaxLength(16)
            .HasDefaultValue("active")
            .IsRequired();

        builder.Property(c => c.Messages)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(c => c.ModelName)
            .HasMaxLength(80);

        builder.Property(c => c.TotalTokens)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.StartedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.EndedAt);

        builder.HasIndex(c => new { c.UserId, c.StartedAt });
        builder.HasIndex(c => c.ScenarioId);
    }
}
