using System.Net;
using System.Net.Http.Json;
using Conduit.Api.Users;
using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests.Users;

[Collection(nameof(Api.Tests.PostgresCollection))]
public sealed class CurrentUserEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private ConduitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public CurrentUserEndpointTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task GetCurrentUser_WithValidToken_Returns200()
    {
        var registered = await RegisterAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/user");
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {registered.User.Token}");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(registered.User.Email, body!.User.Email);
        Assert.Equal(registered.User.Username, body.User.Username);
        Assert.Equal(registered.User.Token, body.User.Token);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_WithBio_Returns200AndUpdatedFields()
    {
        var registered = await RegisterAsync();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/user")
        {
            Content = JsonContent.Create(new UserWrapperRequest<UpdateCurrentUserCommand>
            {
                User = new UpdateCurrentUserCommand
                {
                    Bio = "I like to skateboard",
                    Image = "https://i.stack.imgur.com/xHWG8.jpg"
                }
            })
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {registered.User.Token}");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<UserWrapperResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("I like to skateboard", body!.User.Bio);
        Assert.Equal("https://i.stack.imgur.com/xHWG8.jpg", body.User.Image);
        Assert.False(string.IsNullOrWhiteSpace(body.User.Token));
    }

    [Fact]
    public async Task UpdateCurrentUser_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/user",
            new UserWrapperRequest<UpdateCurrentUserCommand>
            {
                User = new UpdateCurrentUserCommand { Bio = "bio" }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_BlankEmail_Returns422()
    {
        var registered = await RegisterAsync();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/user")
        {
            Content = JsonContent.Create(new UserWrapperRequest<UpdateCurrentUserCommand>
            {
                User = new UpdateCurrentUserCommand { Email = "  " }
            })
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {registered.User.Token}");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ErrorsResponse>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("can't be blank", body!.Errors["email"]);
    }

    private async Task<UserWrapperResponse> RegisterAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync("/api/users", new UserWrapperRequest<RegisterUserCommand>
        {
            User = new RegisterUserCommand
            {
                UserName = $"api_current_{suffix}",
                Email = $"api_current_{suffix}@example.com",
                Password = "jakejake"
            }
        });

        return (await response.Content.ReadFromJsonAsync<UserWrapperResponse>())!;
    }
}
