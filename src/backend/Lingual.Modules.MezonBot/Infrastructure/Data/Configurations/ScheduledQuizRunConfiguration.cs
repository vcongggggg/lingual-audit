using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class ScheduledQuizRunConfiguration : IEntityTypeConfiguration<ScheduledQuizRun>
{
    public void Configure(EntityTypeBuilder<ScheduledQuizRun> builder)
    {
        builder.ToTable("scheduled_quiz_runs");

        builder.HasKey(r => r.SlotKey);

        builder.Property(r => r.SlotKey)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.ChannelId)
            .IsRequired();

        builder.Property(r => r.SessionId);

        builder.Property(r => r.PinnedMessageId);

        builder.Property(r => r.WarnedAt);

        builder.Property(r => r.PostedAt);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(r => r.ChannelId);
    }
}
