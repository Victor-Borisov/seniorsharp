using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;
using SeniorSharp.Persistence.Conversions;

namespace SeniorSharp.Persistence.Configurations;

public sealed class SkillNodeConfiguration : IEntityTypeConfiguration<SkillNode>
{
    public void Configure(EntityTypeBuilder<SkillNode> builder)
    {
        builder.ToTable("skill_nodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(128);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Layer).IsRequired();
        builder.Property(x => x.Description);
        builder.Property(x => x.SeniorSignal);
        builder.Property(x => x.ExampleProbe);
        builder.Property(x => x.GraphVersion).HasMaxLength(64);

        builder.Property(x => x.Axes)
            .HasConversion(StringArrayConverters.Converter)
            .Metadata.SetValueComparer(StringArrayConverters.Comparer);
        builder.Property(x => x.Axes).HasColumnType("jsonb");

        builder.Property(x => x.Prerequisites)
            .HasConversion(StringArrayConverters.Converter)
            .Metadata.SetValueComparer(StringArrayConverters.Comparer);
        builder.Property(x => x.Prerequisites).HasColumnType("jsonb");

        builder.Property(x => x.MasteryFocus)
            .HasConversion(StringArrayConverters.Converter)
            .Metadata.SetValueComparer(StringArrayConverters.Comparer);
        builder.Property(x => x.MasteryFocus).HasColumnType("jsonb");

        builder.HasIndex(x => x.Layer);
        builder.HasIndex(x => x.GraphVersion);
    }
}
