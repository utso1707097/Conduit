using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Conduit.Api.Contracts;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class ListRefreshTokensEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ListRefreshTokensEndpointTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task ListRefreshTokens_AuthenticatedOwner_Returns200()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_list_{suffix}",
                Email = $"api_list_{suffix}@example.com",
                Password = "jakejake"
            }
        });
        var registered = await register.Content.ReadFromJsonAsync<UserWrapperResponse>();
        var userId = ParseSubFromJwt(registered!.User.Token)!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}/refresh-tokens");
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {registered.User.Token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<List<RefreshTokenResponse>>();
        Assert.NotEmpty(tokens!);
    }

    [Fact]
    public async Task ListRefreshTokens_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}/refresh-tokens");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListRefreshTokens_OtherUsersId_Returns403()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = await RegisterAsync($"api_list_a_{suffix}", $"api_list_a_{suffix}@example.com");
        var second = await RegisterAsync($"api_list_b_{suffix}", $"api_list_b_{suffix}@example.com");
        var secondUserId = ParseSubFromJwt(second.User.Token)!;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{secondUserId}/refresh-tokens");
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {first.User.Token}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListRefreshTokens_AfterRefresh_IncludesRevokedAndActiveTokens()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registered = await RegisterAsync(
            $"api_list_hist_{suffix}",
            $"api_list_hist_{suffix}@example.com");
        var userId = ParseSubFromJwt(registered.User.Token)!;
        var originalRefresh = registered.User.RefreshToken!;

        await _client.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest
        {
            RefreshToken = originalRefresh
        });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}/refresh-tokens");
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {registered.User.Token}");
        var response = await _client.SendAsync(request);
        var tokens = await response.Content.ReadFromJsonAsync<List<RefreshTokenResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(tokens!, t => t.Token == originalRefresh && !t.IsActive);
        Assert.Contains(tokens!, t => t.IsActive);
    }

    [Fact]
    public async Task Register_ResponseIncludesRefreshToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registered = await RegisterAsync(
            $"api_reg_refresh_{suffix}",
            $"api_reg_refresh_{suffix}@example.com");

        Assert.False(string.IsNullOrWhiteSpace(registered.User.RefreshToken));
        Assert.NotNull(registered.User.RefreshTokenExpiration);
    }

    private async Task<UserWrapperResponse> RegisterAsync(string username, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = username,
                Email = email,
                Password = "jakejake"
            }
        });

        return (await response.Content.ReadFromJsonAsync<UserWrapperResponse>())!;
    }

    private static string? ParseSubFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(payload);
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
        return doc.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
    }
}
