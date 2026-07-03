using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (controllers, middleware, validators, seeder)
/// against an in-memory Sqlite database instead of PostgreSQL. The single
/// connection is kept open for the lifetime of the factory so the in-memory
/// schema survives across scopes.
/// </summary>
public class TrainingPlatformApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TrainingPlatformDbContext>>();
            services.RemoveAll<DbContextOptions>();

            _connection.Open();
            services.AddDbContext<TrainingPlatformDbContext>(options => options.UseSqlite(_connection));

            ConfigureTestServices(services);
        });
    }

    /// <summary>Hook for derived factories to swap services (e.g. a stubbed code runner).</summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
