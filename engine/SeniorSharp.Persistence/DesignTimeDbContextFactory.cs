using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SeniorSharp.Persistence;

/// <summary>
/// Used by EF Core CLI tooling (dotnet ef migrations / database update) at design time.
/// Connection string is read from the SENIORSHARP_DB env var, falling back to the
/// docker-compose defaults so that `dotnet ef` works out of the box locally.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=seniorsharp;Username=seniorsharp;Password=seniorsharp";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SENIORSHARP_DB") ?? DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
