using Conduit.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Conduit.Api.Tests;

public sealed class ConduitWebApplicationFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgres.ConnectionString,
                ["JWT:Key"] = "test-signing-key-must-be-at-least-32-characters-long",
                ["JWT:Issuer"] = "ConduitTests",
                ["JWT:Audience"] = "ConduitTests",
                ["JWT:DurationInMinutes"] = "15"
            });
        });
    }
}
