using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class ScheduledQuizParticipationConfiguration : IEntityTypeConfiguration<ScheduledQuizParticipation>
{
    public void Configure(EntityTypeBuilder<ScheduledQuizParticipation> builder)
    {
        builder.ToTable("scheduled_quiz_participations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.SlotKey)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.MezonUserId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.ClanId)
            .IsRequired();

        builder.Property(p => p.ChannelId)
            .IsRequired();

        builder.Property(p => p.SessionId)
            .IsRequired();

        builder.Property(p => p.IsCorrect)
            .IsRequired();

        builder.Property(p => p.XpAwarded)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // UNIQUE constraint on (SlotKey, UserId) - Đảm bảo 1 user chỉ nhận XP tối đa 1 lần / khung giờ liên Clan
        builder.HasIndex(p => new { p.SlotKey, p.UserId })
            .IsUnique();

        builder.HasIndex(p => new { p.SessionId, p.UserId });
    }
}
