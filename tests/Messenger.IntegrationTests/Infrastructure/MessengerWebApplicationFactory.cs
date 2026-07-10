using Messenger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Messenger.IntegrationTests.Infrastructure;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TProgram}"/> that:
/// <list type="bullet">
///   <item>Starts a real PostgreSQL container via Testcontainers.</item>
///   <item>Starts a real Redis container via Testcontainers.</item>
///   <item>Replaces the application's DB and Redis registrations with the container connection strings.</item>
///   <item>Runs EF Core migrations to create the schema before tests.</item>
/// </list>
/// </summary>
public sealed class MessengerWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    // ── Testcontainers ────────────────────────────────────────────────────────
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("messenger_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    // ── Connection strings (resolved after containers start) ─────────────────
    public string PostgresConnectionString => _postgres.GetConnectionString();
    public string RedisConnectionString => _redis.GetConnectionString();

    // ── IAsyncLifetime ────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        // Start both containers in parallel
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
    }

    // ── ConfigureWebHost ──────────────────────────────────────────────────────
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── Replace PostgreSQL DbContext ──────────────────────────────────
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(PostgresConnectionString));

            // ── Replace Redis connection ───────────────────────────────────────
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(RedisConnectionString));

            // ── Mock Telegram / SMS ───────────────────────────────────────────
            services.RemoveAll<Messenger.Application.Common.Interfaces.ISmsService>();
            services.AddScoped<Messenger.Application.Common.Interfaces.ISmsService, Messenger.Infrastructure.Services.ConsoleSmsService>();
            
            // Remove TelegramBotListener to prevent it from polling during tests
            var descriptor = services.FirstOrDefault(d => 
                d.ImplementationType == typeof(Messenger.Infrastructure.Services.Telegram.TelegramBotListener));
            if (descriptor != null)
                services.Remove(descriptor);

            // ── Run migrations & ensure schema exists ─────────────────────────
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Test");
    }
}
