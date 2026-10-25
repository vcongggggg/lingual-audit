using Lingual.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Identity.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.ActorId);

        builder.Property(a => a.Action)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.EntityId);

        builder.Property(a => a.Reason);

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.ActorId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });
    }
}
