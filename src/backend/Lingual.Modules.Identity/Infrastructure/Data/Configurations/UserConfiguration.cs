using Lingual.Modules.Identity.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Identity.Infrastructure.Data.Configurations;

public class UserConfiguration : BaseConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.MezonUserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(u => u.MezonUserId)
            .IsUnique();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(u => u.AvatarUrl);

        builder.Property(u => u.PreferredLocale)
            .HasMaxLength(16)
            .HasDefaultValue("vi-VN")
            .IsRequired();

        builder.Property(u => u.Timezone)
            .HasMaxLength(64)
            .HasDefaultValue("Asia/Ho_Chi_Minh")
            .IsRequired();

        builder.Property(u => u.Status)
            .HasMaxLength(20)
            .HasDefaultValue("active")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasMaxLength(20)
            .HasDefaultValue("learner")
            .IsRequired();

        builder.Property(u => u.LastSeenAt);

        builder.Property(u => u.DeletedAt);

        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<LearnerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PlacementTests)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
