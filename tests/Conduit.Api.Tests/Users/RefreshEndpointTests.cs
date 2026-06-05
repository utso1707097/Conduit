using System.Net;
using System.Net.Http.Json;
using Conduit.Api.Contracts;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class RefreshEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public RefreshEndpointTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Refresh_ValidRefreshToken_Returns200WithNewTokens()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"api_refresh_{suffix}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_refresh_{suffix}",
                Email = email,
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();
        var refreshToken = registered!.User.RefreshToken!;

        var response = await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();
        Assert.NotEqual(refreshToken, body!.User.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(body.User.Token));
    }

    [Fact]
    public async Task Refresh_BlankToken_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest
        {
            RefreshToken = ""
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest
        {
            RefreshToken = "missing-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusedTokenAfterRotation_Returns401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_reuse_{suffix}",
                Email = $"api_reuse_{suffix}@example.com",
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();
        var original = registered!.User.RefreshToken!;

        await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest { RefreshToken = original });
        var second = await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest { RefreshToken = original });

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Login_ResponseIncludesRefreshToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"api_login_refresh_{suffix}@example.com";
        await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_login_refresh_{suffix}",
                Email = email,
                Password = "jakejake"
            }
        });

        var response = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest { Email = email, Password = "jakejake" }
        });

        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.User.RefreshToken));
        Assert.NotNull(body.User.RefreshTokenExpiration);
    }
}
