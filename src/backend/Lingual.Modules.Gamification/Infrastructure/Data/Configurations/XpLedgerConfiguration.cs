using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class XpLedgerConfiguration : IEntityTypeConfiguration<XpLedger>
{
    public void Configure(EntityTypeBuilder<XpLedger> builder)
    {
        builder.ToTable("xp_ledger");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SourceType)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.SourceId);

        builder.Property(x => x.XpDelta)
            .IsRequired();

        builder.Property(x => x.AwardedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(200);
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.AwardedAt });
        builder.HasIndex(x => new { x.SourceType, x.SourceId });
    }
}
