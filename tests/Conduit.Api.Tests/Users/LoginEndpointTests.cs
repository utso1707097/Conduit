using System.Net;
using System.Net.Http.Json;
using Conduit.Api.Contracts;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class LoginEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public LoginEndpointTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"api_login_ok_{suffix}@example.com";
        const string password = "jakejake";
        await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_login_ok_{suffix}",
                Email = email,
                Password = password
            }
        });

        var response = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest { Email = email, Password = password }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.User.Token));
        Assert.Equal(email, body!.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"api_login_bad_{suffix}@example.com";
        await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_login_bad_{suffix}",
                Email = email,
                Password = "jakejake"
            }
        });

        var response = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest { Email = email, Password = "wrong-password" }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("is invalid", body!.Errors["email or password"]);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest
            {
                Email = $"missing_{Guid.NewGuid():N}@example.com",
                Password = "jakejake"
            }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("is invalid", body!.Errors["email or password"]);
    }

    [Fact]
    public async Task Login_MissingFields_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest { Email = "", Password = "" }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("can't be blank", body!.Errors["email"]);
        Assert.Contains("can't be blank", body.Errors["password"]);
    }

    [Fact]
    public async Task Login_AfterRegister_ReturnsTokenForNewUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"api_roundtrip_{suffix}";
        var email = $"api_roundtrip_{suffix}@example.com";
        const string password = "jakejake";

        var registerResponse = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = userName,
                Email = email,
                Password = password
            }
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new UserWrapperRequest<LoginUserRequest>
        {
            User = new LoginUserRequest { Email = email, Password = password }
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<UserWrapperResponse>();
        Assert.Equal(userName, loginBody!.User.Username);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.User.Token));
    }
}
