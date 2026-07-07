using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Messenger.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations) to create the DbContext
/// without requiring the full application startup (no Redis, no SMS, etc.).
/// This class is only invoked by <c>dotnet ef migrations add/update</c>.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Try to read from appsettings, fall back to a design-time default
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",
                    "Messenger.WebApi",
                    "appsettings.json"),
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=messenger_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
