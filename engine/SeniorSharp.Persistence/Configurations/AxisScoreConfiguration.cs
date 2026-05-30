using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;
using SeniorSharp.Persistence.Conversions;

namespace SeniorSharp.Persistence.Configurations;

public sealed class AxisScoreConfiguration : IEntityTypeConfiguration<AxisScore>
{
    public void Configure(EntityTypeBuilder<AxisScore> builder)
    {
        builder.ToTable("axis_scores");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Axis).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Level).HasMaxLength(64);
        builder.Property(x => x.Score);
        builder.Property(x => x.Rationale);
        builder.Property(x => x.RunIndex);

        builder.Property(x => x.Citations)
            .HasConversion(StringArrayConverters.Converter)
            .Metadata.SetValueComparer(StringArrayConverters.Comparer);
        builder.Property(x => x.Citations).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.SessionId, x.Axis, x.RunIndex });
    }
}
