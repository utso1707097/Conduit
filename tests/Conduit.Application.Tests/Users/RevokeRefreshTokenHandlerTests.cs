using Conduit.Application.Common;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;
using Xunit;

namespace Conduit.Application.Tests.Users;

public sealed class RevokeRefreshTokenHandlerTests
{
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly RevokeRefreshTokenHandler _handler;

    public RevokeRefreshTokenHandlerTests()
    {
        _handler = new RevokeRefreshTokenHandler(_refreshTokens);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_RevokesSuccessfully()
    {
        var issued = (await _refreshTokens.IssueAsync("user-1")).Value!;

        var result = await _handler.HandleAsync(new RevokeRefreshTokenCommand { RefreshToken = issued.Token });

        Assert.True(result.IsSuccess);
        var found = await _refreshTokens.FindByTokenAsync(issued.Token);
        Assert.NotNull(found);
        Assert.False(found!.IsActive);
    }

    [Fact]
    public async Task HandleAsync_BlankToken_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new RevokeRefreshTokenCommand { RefreshToken = "" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new RevokeRefreshTokenCommand { RefreshToken = "missing" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_AlreadyRevokedToken_ReturnsValidationError()
    {
        var issued = (await _refreshTokens.IssueAsync("user-1")).Value!;
        await _refreshTokens.RevokeAsync(issued.Token);

        var result = await _handler.HandleAsync(new RevokeRefreshTokenCommand { RefreshToken = issued.Token });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_TokenWithWhitespace_TrimsBeforeLookup()
    {
        var issued = (await _refreshTokens.IssueAsync("user-1")).Value!;

        var result = await _handler.HandleAsync(new RevokeRefreshTokenCommand { RefreshToken = $"  {issued.Token}  " });

        Assert.True(result.IsSuccess);
    }
}
