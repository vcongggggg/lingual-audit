using Lingual.Modules.Identity.Domain;
using Lingual.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Identity.Infrastructure.Data.Configurations;

public class LearnerProfileConfiguration : BaseConfiguration<LearnerProfile>
{
    public override void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        base.Configure(builder);

        builder.ToTable("learner_profiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.LearningGoal)
            .HasMaxLength(32);

        builder.Property(p => p.DailyCommitmentMinutes);

        builder.Property(p => p.ProficiencyLevel)
            .HasMaxLength(2);

        builder.Property(p => p.PlacementStatus)
            .HasMaxLength(16)
            .HasDefaultValue("pending")
            .IsRequired();

        builder.Property(p => p.OnboardingCompletedAt);
    }
}
