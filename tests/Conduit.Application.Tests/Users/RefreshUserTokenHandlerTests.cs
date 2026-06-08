using Conduit.Application.Common;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;
using Xunit;

namespace Conduit.Application.Tests.Users;

public sealed class RefreshUserTokenHandlerTests
{
    private readonly FakeUserAccountStore _users = new();
    private readonly FakeTokenIssuer _tokens = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly RefreshUserTokenHandler _handler;

    public RefreshUserTokenHandlerTests()
    {
        _handler = new RefreshUserTokenHandler(_users, _tokens, _refreshTokens);
    }

    [Fact]
    public async Task HandleAsync_ValidRefreshToken_ReturnsNewJwtAndRotatedRefreshToken()
    {
        var account = (await _users.CreateAsync("jake", "jake@jake.jake", "jakejake")).Value!;
        var issued = (await _refreshTokens.IssueAsync(account.Id)).Value!;

        var result = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = issued.Token });

        Assert.True(result.IsSuccess);
        Assert.NotEqual(issued.Token, result.Value!.RefreshToken.Token);
        Assert.StartsWith("test-token-for-", result.Value.Token);
    }

    [Fact]
    public async Task HandleAsync_BlankToken_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = "" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["refreshToken"]);
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_ReturnsUnauthorized()
    {
        var result = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = "missing-token" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_RevokedToken_ReturnsUnauthorized()
    {
        var account = (await _users.CreateAsync("jake", "jake@jake.jake", "jakejake")).Value!;
        var issued = (await _refreshTokens.IssueAsync(account.Id)).Value!;
        await _refreshTokens.RevokeAsync(issued.Token);

        var result = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = issued.Token });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_ReusedOldTokenAfterRotation_ReturnsUnauthorized()
    {
        var account = (await _users.CreateAsync("jake", "jake@jake.jake", "jakejake")).Value!;
        var issued = (await _refreshTokens.IssueAsync(account.Id)).Value!;
        var firstRefresh = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = issued.Token });
        Assert.True(firstRefresh.IsSuccess);

        var secondRefresh = await _handler.HandleAsync(new RefreshTokenCommand { RefreshToken = issued.Token });

        Assert.False(secondRefresh.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, secondRefresh.Kind);
    }
}
