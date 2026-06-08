using System.Net;
using System.Net.Http.Json;
using Conduit.Api.Users;
using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class RevokeRefreshEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public RevokeRefreshEndpointTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync()
    {
        _factory = new ConduitWebApplicationFactory(_postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task RevokeRefresh_ValidToken_Returns200()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserCommand>
        {
            User = new RegisterUserCommand
            {
                UserName = $"api_revoke_{suffix}",
                Email = $"api_revoke_{suffix}@example.com",
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();

        var response = await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = registered!.User.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.Equal("Token revoked", body!.Message);
    }

    [Fact]
    public async Task RevokeRefresh_BlankToken_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = ""
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task RevokeRefresh_UnknownToken_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = "missing-token"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeRefresh_AfterRevoke_RefreshFailsWith401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserCommand>
        {
            User = new RegisterUserCommand
            {
                UserName = $"api_revoke_flow_{suffix}",
                Email = $"api_revoke_flow_{suffix}@example.com",
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();
        var refreshToken = registered!.User.RefreshToken!;

        await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = refreshToken
        });

        var refresh = await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenCommand
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RevokeRefresh_AlreadyRevokedToken_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserCommand>
        {
            User = new RegisterUserCommand
            {
                UserName = $"api_revoke_twice_{suffix}",
                Email = $"api_revoke_twice_{suffix}@example.com",
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();
        var refreshToken = registered!.User.RefreshToken!;

        await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = refreshToken
        });

        var second = await _client.PostAsJsonAsync("/api/users/revoke-refresh", new RevokeRefreshTokenCommand
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }
}
