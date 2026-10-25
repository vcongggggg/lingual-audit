using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class DuelMatchConfiguration : IEntityTypeConfiguration<DuelMatch>
{
    public void Configure(EntityTypeBuilder<DuelMatch> builder)
    {
        builder.ToTable("duel_matches");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.ChallengerId)
            .IsRequired();

        builder.Property(d => d.OpponentId)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(16)
            .HasDefaultValue("pending")
            .IsRequired();

        builder.Property(d => d.QuestionCount)
            .HasDefaultValue((short)5)
            .IsRequired();

        builder.Property(d => d.QuestionIds)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(d => d.ChallengerAnswers)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(d => d.OpponentAnswers)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(d => d.ChallengerScore)
            .HasPrecision(8, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(d => d.OpponentScore)
            .HasPrecision(8, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(d => d.ChallengerCorrect)
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(d => d.OpponentCorrect)
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(d => d.WinnerId);

        builder.Property(d => d.StartedAt);
        builder.Property(d => d.CompletedAt);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(d => new { d.ChallengerId, d.CreatedAt });
        builder.HasIndex(d => new { d.OpponentId, d.CreatedAt });
        builder.HasIndex(d => d.WinnerId);
    }
}
