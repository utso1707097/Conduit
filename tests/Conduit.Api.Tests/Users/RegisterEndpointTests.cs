using System.Net;
using System.Net.Http.Json;
using Conduit.Api.Contracts;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class RegisterEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public RegisterEndpointTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Register_ValidRequest_Returns201WithToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"api_reg_{suffix}",
                Email = $"api_reg_{suffix}@example.com",
                Password = "jakejake"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();
        Assert.NotNull(body?.User.Token);
        Assert.Equal(request.User.Username, body.User.Username);
        Assert.Equal(request.User.Email, body.User.Email);
    }

    [Fact]
    public async Task Register_MissingFields_Returns422()
    {
        var request = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = "",
                Email = "",
                Password = ""
            }
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("can't be blank", body!.Errors["username"]);
        Assert.Contains("can't be blank", body.Errors["email"]);
        Assert.Contains("can't be blank", body.Errors["password"]);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"api_dup_email_{suffix}@example.com";
        var first = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"first_{suffix}",
                Email = email,
                Password = "jakejake"
            }
        };
        var second = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = $"second_{suffix}",
                Email = email,
                Password = "jakejake"
            }
        };

        await _client.PostAsJsonAsync("/api/users", first);
        var response = await _client.PostAsJsonAsync("/api/users", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("has already been taken", body!.Errors["email"]);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"api_dup_user_{suffix}";
        var first = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = userName,
                Email = $"one_{suffix}@example.com",
                Password = "jakejake"
            }
        };
        var second = new UserWrapperRequest<RegisterUserRequest>
        {
            User = new RegisterUserRequest
            {
                Username = userName,
                Email = $"two_{suffix}@example.com",
                Password = "jakejake"
            }
        };

        await _client.PostAsJsonAsync("/api/users", first);
        var response = await _client.PostAsJsonAsync("/api/users", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("has already been taken", body!.Errors["username"]);
    }

    [Fact]
    public async Task Register_EmptyBody_Returns422()
    {
        var response = await _client.PostAsJsonAsync<UserWrapperRequest<RegisterUserRequest>?>(
            "/api/users",
            null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();
        Assert.Contains("can't be blank", body!.Errors["username"]);
        Assert.Contains("can't be blank", body.Errors["email"]);
        Assert.Contains("can't be blank", body.Errors["password"]);
    }
}
