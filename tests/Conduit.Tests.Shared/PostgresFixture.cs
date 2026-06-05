using Conduit.Infrastructure;
using Conduit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conduit.Tests.Shared;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConduitDbContext>();
        await db.Database.MigrateAsync();
        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();

    public AsyncServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["JWT:Key"] = "test-signing-key-must-be-at-least-32-characters-long",
                ["JWT:Issuer"] = "ConduitTests",
                ["JWT:Audience"] = "ConduitTests",
                ["JWT:DurationInMinutes"] = "15",
                ["JWT:RefreshTokenDurationInDays"] = "10"
            })
            .Build();

        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider().CreateAsyncScope();
    }
}
