using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence.Configurations;

public sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("rounds");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Order);
        builder.Property(x => x.Status).HasMaxLength(32);
        builder.Property(x => x.PendingSkillId).HasMaxLength(128);

        builder.HasMany(x => x.Turns)
            .WithOne()
            .HasForeignKey(t => t.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.Order });
    }
}
