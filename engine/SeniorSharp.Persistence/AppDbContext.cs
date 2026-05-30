using Microsoft.EntityFrameworkCore;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence;

/// <summary>
/// EF Core context for SeniorSharp. Configured for PostgreSQL (Npgsql).
/// Entity configuration lives in dedicated <c>IEntityTypeConfiguration&lt;T&gt;</c> classes
/// that are picked up via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SkillNode> SkillNodes => Set<SkillNode>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Turn> Turns => Set<Turn>();
    public DbSet<SkillMastery> SkillMasteries => Set<SkillMastery>();
    public DbSet<AxisScore> AxisScores => Set<AxisScore>();
    public DbSet<Verdict> Verdicts => Set<Verdict>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All IEntityTypeConfiguration<T> in this assembly (Configurations/*.cs).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
