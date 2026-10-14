using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class WeeklyLeaderboardConfiguration : IEntityTypeConfiguration<WeeklyLeaderboard>
{
    public void Configure(EntityTypeBuilder<WeeklyLeaderboard> builder)
    {
        builder.ToTable("weekly_leaderboards");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(w => w.WeekStart)
            .IsRequired();

        builder.Property(w => w.WeekEnd)
            .IsRequired();

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.XpTotal)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(w => w.Rank);

        builder.Property(w => w.Status)
            .HasMaxLength(16)
            .HasDefaultValue("open")
            .IsRequired();

        builder.Property(w => w.FinalizedAt);

        builder.Property(w => w.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(w => new { w.WeekStart, w.UserId })
            .IsUnique();

        builder.HasIndex(w => new { w.WeekStart, w.Rank });
        builder.HasIndex(w => new { w.WeekStart, w.XpTotal });
    }
}
