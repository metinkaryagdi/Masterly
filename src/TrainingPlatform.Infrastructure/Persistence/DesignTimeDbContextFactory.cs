using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrainingPlatform.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` create the context without booting the API host. Migration
/// generation only needs the model, so the connection string is never opened —
/// it just has to be a syntactically valid Npgsql string.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainingPlatformDbContext>
{
    public TrainingPlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TrainingPlatformDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=training_platform;Username=postgres;Password=postgres")
            .Options;

        return new TrainingPlatformDbContext(options);
    }
}
