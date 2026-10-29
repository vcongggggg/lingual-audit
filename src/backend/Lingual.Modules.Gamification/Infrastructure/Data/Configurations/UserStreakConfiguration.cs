using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
    public void Configure(EntityTypeBuilder<UserStreak> builder)
    {
        builder.ToTable("user_streaks");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.CurrentDays)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.BestDays)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.LastActiveDate);

        builder.Property(u => u.FreezeBalance)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.FreezeHistory)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(u => u.DailyActivity)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
