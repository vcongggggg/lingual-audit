using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class GameQuestionConfiguration : IEntityTypeConfiguration<GameQuestion>
{
    public void Configure(EntityTypeBuilder<GameQuestion> builder)
    {
        builder.ToTable("game_questions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasMaxLength(64);

        builder.Property(q => q.GameType).HasMaxLength(32).IsRequired();
        builder.Property(q => q.Prompt).HasColumnType("text").IsRequired();
        builder.Property(q => q.ChoicesJson).HasColumnType("jsonb");
        builder.Property(q => q.Answer).HasColumnType("text").IsRequired();
        builder.Property(q => q.Level).HasMaxLength(8).HasDefaultValue("A2").IsRequired();
        builder.Property(q => q.Source).HasMaxLength(64).HasDefaultValue("script-derived").IsRequired();
        builder.Property(q => q.SourceRef).HasColumnType("text");
        builder.Property(q => q.CreatedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(q => q.UpdatedAt).HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(q => q.GameType);
        builder.HasIndex(q => q.Level);
    }
}
