using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class ActiveGameAttemptConfiguration : IEntityTypeConfiguration<ActiveGameAttempt>
{
    public void Configure(EntityTypeBuilder<ActiveGameAttempt> builder)
    {
        builder.ToTable("active_game_attempts");

        builder.HasKey(a => a.AttemptId);
        builder.Property(a => a.AttemptId).HasMaxLength(64);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.GameType).HasMaxLength(32).IsRequired();
        builder.Property(a => a.StartedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(a => a.QuestionsPayloadJson).HasColumnType("text").IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(a => a.UserId);
    }
}
