using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("game_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.AttemptId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.GameType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.Score)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.Accuracy)
            .HasDefaultValue(0.0)
            .IsRequired();

        builder.Property(s => s.XpEarned)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.DurationSeconds)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.ComboMax)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(s => s.MetadataJson)
            .HasColumnType("text");

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(s => s.AttemptId).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.GameType, s.Score });
    }
}
