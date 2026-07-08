using MediatR;
using Messenger.Application.Common.Interfaces;
using Messenger.Domain.Interfaces;
using Messenger.Infrastructure.Persistence;
using Messenger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Messenger.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure-layer services into the DI container.
/// Called once from <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL / EF Core ────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                });
        });

        // Register the DbContext also as the IApplicationDbContext abstraction
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // ── Redis ──────────────────────────────────────────────────────────────
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        // ── Services ───────────────────────────────────────────────────────────
        services.AddScoped<IOtpService, RedisOtpService>();
        services.AddScoped<IStorageService, DummyStorageService>();
        services.AddScoped<IPushNotificationService, DummyPushNotificationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ISmsService, ConsoleSmsService>();

        // ── JWT Settings ───────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // ── Infrastructure MediatR Handlers (SignalR notification handlers) ────
        // Register notification handlers defined in this assembly (e.g. MessageSentNotificationHandler)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the database.
    /// Keeps ApplicationDbContext encapsulated inside the Infrastructure layer.
    /// </summary>
    public static void InitializeDatabase(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}
