using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeCraftNet.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` create the context without booting the API host. Migration
/// generation only needs the model, so the connection string is never opened —
/// it just has to be a syntactically valid Npgsql string.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CodeCraftNetDbContext>
{
    public CodeCraftNetDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CodeCraftNetDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=codecraftnet_db;Username=postgres;Password=postgres")
            .Options;

        return new CodeCraftNetDbContext(options);
    }
}
