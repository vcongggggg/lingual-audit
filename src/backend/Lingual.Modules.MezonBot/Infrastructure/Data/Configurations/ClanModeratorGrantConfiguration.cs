using Lingual.Modules.MezonBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class ClanModeratorGrantConfiguration : IEntityTypeConfiguration<ClanModeratorGrant>
{
    public void Configure(EntityTypeBuilder<ClanModeratorGrant> builder)
    {
        builder.ToTable("clan_moderator_grants");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.GrantedBy);

        builder.Property(c => c.GrantedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.ExpiresAt);
        builder.Property(c => c.RevokedAt);
        builder.Property(c => c.RevokedBy);
        builder.Property(c => c.Reason);

        builder.HasIndex(c => c.UserId);
    }
}
