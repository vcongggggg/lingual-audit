using Lingual.Modules.AITutor.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.AITutor.Infrastructure.Data.Configurations;

public class AiCreditWalletConfiguration : IEntityTypeConfiguration<AiCreditWallet>
{
    public void Configure(EntityTypeBuilder<AiCreditWallet> builder)
    {
        builder.ToTable("ai_credit_wallets", t =>
        {
            t.HasCheckConstraint("ck_ai_credit_wallets_balance_non_negative", "balance >= 0");
            t.HasCheckConstraint("ck_ai_credit_wallets_daily_free_non_negative", "daily_free_remaining >= 0");
            t.HasCheckConstraint("ck_ai_credit_wallets_daily_bonus_non_negative", "daily_bonus_earned >= 0");
        });

        builder.HasKey(w => w.UserId);

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.Balance)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(w => w.DailyFreeRemaining)
            .HasDefaultValue(5)
            .IsRequired();

        builder.Property(w => w.LastResetDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(w => w.DailyBonusEarned)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
