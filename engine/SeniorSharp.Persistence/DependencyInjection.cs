using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SeniorSharp.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> (Npgsql/PostgreSQL) and the <see cref="GraphSeeder"/>.
    /// </summary>
    public static IServiceCollection AddSeniorSharpPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<GraphSeeder>();

        return services;
    }
}
