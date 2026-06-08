using Conduit.Application.Common;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;
using Xunit;

namespace Conduit.Application.Tests.Users;

public sealed class GetCurrentUserHandlerTests
{
    private readonly FakeUserAccountStore _store = new();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests() => _handler = new GetCurrentUserHandler(_store);

    [Fact]
    public async Task HandleAsync_KnownUserId_ReturnsUserAccount()
    {
        var created = await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");

        var result = await _handler.HandleAsync(new GetCurrentUserQuery(created.Value!.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("jake", result.Value!.UserName);
        Assert.Equal("jake@jake.jake", result.Value.Email);
    }

    [Fact]
    public async Task HandleAsync_UnknownUserId_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new GetCurrentUserQuery(Guid.NewGuid().ToString()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
        Assert.Contains("was not found", result.Errors["user"]);
    }

    [Fact]
    public async Task HandleAsync_BlankUserId_ReturnsUnauthorized()
    {
        var result = await _handler.HandleAsync(new GetCurrentUserQuery(""));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }
}
