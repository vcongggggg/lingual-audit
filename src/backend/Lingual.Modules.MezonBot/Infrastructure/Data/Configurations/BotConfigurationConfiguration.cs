using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class BotConfigurationConfiguration : IEntityTypeConfiguration<BotConfiguration>
{
    public void Configure(EntityTypeBuilder<BotConfiguration> builder)
    {
        builder.ToTable("bot_configuration");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasDefaultValue((short)1);

        builder.Property(b => b.MezonClanId)
            .HasMaxLength(128)
            .IsRequired();
        builder.HasIndex(b => b.MezonClanId)
            .IsUnique();

        builder.Property(b => b.ClanNameSnapshot)
            .HasMaxLength(200);

        builder.Property(b => b.DefaultChannelMezonId)
            .HasMaxLength(128);

        builder.Property(b => b.Status)
            .HasMaxLength(16)
            .HasDefaultValue("active")
            .IsRequired();

        builder.Property(b => b.Timezone)
            .HasMaxLength(64)
            .HasDefaultValue("Asia/Ho_Chi_Minh")
            .IsRequired();

        builder.Property(b => b.InstalledBy);

        builder.Property(b => b.InstalledAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(b => b.UninstalledAt);

        builder.Property(b => b.DailyWordEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(b => b.QuizEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(b => b.QuizDurationSeconds)
            .HasDefaultValue((short)30)
            .IsRequired();

        builder.Property(b => b.Schedules)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
