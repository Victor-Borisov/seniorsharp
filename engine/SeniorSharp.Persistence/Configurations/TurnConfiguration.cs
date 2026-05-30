using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence.Configurations;

public sealed class TurnConfiguration : IEntityTypeConfiguration<Turn>
{
    public void Configure(EntityTypeBuilder<Turn> builder)
    {
        builder.ToTable("turns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.CreatedAt);

        builder.HasIndex(x => new { x.RoundId, x.CreatedAt });

        // Turns are append-only and immutable by design: once written, a Turn must never be
        // updated or deleted (the transcript is the audit trail / evidence source for scoring).
        // This invariant is enforced at the application/repository layer; EF Core has no native
        // "insert-only" constraint. A DB-level trigger blocking UPDATE/DELETE on "turns" can be
        // added in a migration as defense-in-depth.
        // TODO: add immutability trigger (BEFORE UPDATE OR DELETE -> RAISE EXCEPTION) in a migration.
    }
}
