using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence.Configurations;

public sealed class VerdictConfiguration : IEntityTypeConfiguration<Verdict>
{
    public void Configure(EntityTypeBuilder<Verdict> builder)
    {
        builder.ToTable("verdicts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OverallLevel).HasMaxLength(64);
        builder.Property(x => x.Summary);
        builder.Property(x => x.RunCount);
        builder.Property(x => x.Spread);

        // ProfileJson holds the aggregated mastery/axis profile as JSON.
        builder.Property(x => x.ProfileJson).HasColumnType("jsonb");

        // One verdict per session (final aggregate).
        builder.HasIndex(x => x.SessionId).IsUnique();
    }
}
