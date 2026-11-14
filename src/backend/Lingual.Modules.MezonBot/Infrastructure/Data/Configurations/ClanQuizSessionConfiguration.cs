using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class ClanQuizSessionConfiguration : IEntityTypeConfiguration<ClanQuizSession>
{
    public void Configure(EntityTypeBuilder<ClanQuizSession> builder)
    {
        builder.ToTable("clan_quiz_sessions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.ChannelMezonId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(c => c.QuestionId)
            .IsRequired();

        builder.Property(c => c.StartedBy);

        builder.Property(c => c.Status)
            .HasMaxLength(16)
            .HasDefaultValue("open")
            .IsRequired();

        builder.Property(c => c.OpenedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.ClosesAt)
            .IsRequired();

        builder.Property(c => c.ClosedAt);

        builder.Property(c => c.WinningUserId);

        builder.Property(c => c.WinningResponseMs);

        builder.Property(c => c.ExplanationPublishedAt);

        builder.Property(c => c.Responses)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(c => c.SlotKey)
            .HasMaxLength(32);

        builder.HasOne(c => c.Question)
            .WithMany(q => q.ClanQuizSessions)
            .HasForeignKey(c => c.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ChannelMezonId, c.OpenedAt });
        builder.HasIndex(c => c.QuestionId);
    }
}
