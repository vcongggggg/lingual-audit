using Lingual.Modules.AITutor.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.AITutor.Infrastructure.Data.Configurations;

public class AiCreditTransactionConfiguration : IEntityTypeConfiguration<AiCreditTransaction>
{
    public void Configure(EntityTypeBuilder<AiCreditTransaction> builder)
    {
        builder.ToTable("ai_credit_transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .IsRequired();

        builder.Property(t => t.Reason)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.IdempotencyKey)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(t => new { t.UserId, t.CreatedAt });

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
