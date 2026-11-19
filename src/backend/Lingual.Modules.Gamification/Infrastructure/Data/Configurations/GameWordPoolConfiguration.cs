using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class GameWordPoolConfiguration : IEntityTypeConfiguration<GameWordPool>
{
    public void Configure(EntityTypeBuilder<GameWordPool> builder)
    {
        builder.ToTable("game_word_pools");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasMaxLength(64);

        builder.Property(w => w.TargetText).HasMaxLength(128).IsRequired();
        builder.Property(w => w.Translation).HasMaxLength(256).IsRequired();
        builder.Property(w => w.Phonetic).HasMaxLength(128);
        builder.Property(w => w.Topic).HasMaxLength(64).IsRequired();
        builder.Property(w => w.TopicVi).HasMaxLength(128);
        builder.Property(w => w.CefrLevel).HasMaxLength(8).HasDefaultValue("A1").IsRequired();
        builder.Property(w => w.ExampleSentence).HasMaxLength(512);
        builder.Property(w => w.ExampleTranslation).HasMaxLength(512);
        builder.Property(w => w.Source).HasMaxLength(64).HasDefaultValue("ai-generated").IsRequired();
        builder.Property(w => w.Verified).HasDefaultValue(false).IsRequired();
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(w => w.Topic);
        builder.HasIndex(w => w.CefrLevel);
    }
}
