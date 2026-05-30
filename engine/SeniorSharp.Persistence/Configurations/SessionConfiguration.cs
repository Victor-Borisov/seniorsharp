using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CandidateRef).HasMaxLength(256);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.GraphVersion).HasMaxLength(64);
        builder.Property(x => x.PromptVersion).HasMaxLength(64);
        builder.Property(x => x.ModelId).HasMaxLength(128);

        builder.HasMany(x => x.Rounds)
            .WithOne()
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Mastery)
            .WithOne()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CandidateRef);
    }
}
