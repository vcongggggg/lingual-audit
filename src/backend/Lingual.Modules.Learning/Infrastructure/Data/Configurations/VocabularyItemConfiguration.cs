using Lingual.Modules.Learning.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Learning.Infrastructure.Data.Configurations;

public class VocabularyItemConfiguration : BaseConfiguration<VocabularyItem>
{
    public override void Configure(EntityTypeBuilder<VocabularyItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("vocabulary_items");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(v => v.Term)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(v => v.NormalizedTerm)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(v => v.SourceLanguage)
            .HasMaxLength(16)
            .HasDefaultValue("en")
            .IsRequired();

        builder.Property(v => v.TargetLanguage)
            .HasMaxLength(16)
            .HasDefaultValue("vi")
            .IsRequired();

        builder.Property(v => v.Ipa)
            .HasMaxLength(240);

        builder.Property(v => v.PartOfSpeech)
            .HasMaxLength(32);

        builder.Property(v => v.Meaning)
            .IsRequired();

        builder.Property(v => v.PronunciationAudioUrl);

        builder.Property(v => v.ImageUrl);

        builder.Property(v => v.Examples)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(v => v.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(v => v.CreatedBy);

        builder.HasIndex(v => new { v.NormalizedTerm, v.SourceLanguage, v.TargetLanguage })
            .IsUnique();
    }
}
