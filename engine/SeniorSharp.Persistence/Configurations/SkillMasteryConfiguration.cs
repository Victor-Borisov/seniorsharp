using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;
using SeniorSharp.Persistence.Conversions;

namespace SeniorSharp.Persistence.Configurations;

public sealed class SkillMasteryConfiguration : IEntityTypeConfiguration<SkillMastery>
{
    public void Configure(EntityTypeBuilder<SkillMastery> builder)
    {
        builder.ToTable("skill_masteries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkillId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Recognition);
        builder.Property(x => x.Application);
        builder.Property(x => x.Depth);

        builder.Property(x => x.EvidenceTurnIds)
            .HasConversion(GuidListConverters.Converter)
            .Metadata.SetValueComparer(GuidListConverters.Comparer);
        builder.Property(x => x.EvidenceTurnIds).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.SessionId, x.SkillId }).IsUnique();
    }
}
