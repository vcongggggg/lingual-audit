using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("user_badges");

        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ub => ub.UserId)
            .IsRequired();

        builder.Property(ub => ub.BadgeId)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(ub => ub.IsUnlocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(ub => ub.UnlockedAt);

        builder.Property(ub => ub.CurrentProgress)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(ub => ub.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId })
            .IsUnique();
    }
}
