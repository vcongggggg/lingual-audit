using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class UserGameStatConfiguration : IEntityTypeConfiguration<UserGameStat>
{
    public void Configure(EntityTypeBuilder<UserGameStat> builder)
    {
        builder.ToTable("user_game_stats");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.GameType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.DisplayName)
            .HasMaxLength(128);

        builder.Property(s => s.AvatarUrl)
            .HasColumnType("text");

        builder.Property(s => s.HighScore)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.TotalGamesPlayed)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.TotalWins)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.TotalXpEarned)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.AvgAccuracy)
            .HasDefaultValue(0.0)
            .IsRequired();

        builder.Property(s => s.LastPlayedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(s => new { s.UserId, s.GameType }).IsUnique();
        builder.HasIndex(s => new { s.GameType, s.HighScore });
    }
}
