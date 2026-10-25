using Lingual.Modules.MezonBot.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Configurations;

public class QuizQuestionConfiguration : BaseConfiguration<QuizQuestion>
{
    public override void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        base.Configure(builder);

        builder.ToTable("quiz_questions");

        builder.HasKey(qq => qq.Id);
        builder.Property(qq => qq.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(qq => qq.QuizId);

        builder.Property(qq => qq.QuestionType)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(qq => qq.Prompt)
            .IsRequired();

        builder.Property(qq => qq.PromptAudioUrl);

        builder.Property(qq => qq.Difficulty);

        builder.Property(qq => qq.Level)
            .HasMaxLength(2);

        builder.Property(qq => qq.Options)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(qq => qq.Payload)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(qq => qq.Explanation);

        builder.Property(qq => qq.Points)
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(qq => qq.SortOrder)
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(qq => qq.Status)
            .HasMaxLength(16)
            .HasDefaultValue("published")
            .IsRequired();

        builder.Property(qq => qq.CreatedBy);

        builder.HasIndex(qq => new { qq.QuizId, qq.SortOrder });
        builder.HasIndex(qq => new { qq.Status, qq.Level, qq.QuestionType });
    }
}
