using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence.Configurations;

public sealed class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("prompt_versions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Version).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Hash).HasMaxLength(128);
        builder.Property(x => x.Ref).HasMaxLength(256);

        builder.HasIndex(x => new { x.Key, x.Version }).IsUnique();
    }
}
