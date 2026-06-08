using Conduit.Application.Common;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;
using Xunit;

namespace Conduit.Application.Tests.Users;

public sealed class ListUserRefreshTokensHandlerTests
{
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly ListUserRefreshTokensHandler _handler;

    public ListUserRefreshTokensHandlerTests()
    {
        _handler = new ListUserRefreshTokensHandler(_refreshTokens);
    }

    [Fact]
    public async Task HandleAsync_MatchingRequester_ReturnsUserTokens()
    {
        await _refreshTokens.IssueAsync("user-1");
        await _refreshTokens.IssueAsync("user-1");

        var result = await _handler.HandleAsync(new ListRefreshTokensCommand("user-1", "user-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Count >= 1);
    }

    [Fact]
    public async Task HandleAsync_BlankUserId_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new ListRefreshTokensCommand("", "user-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_RequesterMismatch_ReturnsForbidden()
    {
        var result = await _handler.HandleAsync(new ListRefreshTokensCommand("user-1", "user-2"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_NoTokens_ReturnsEmptyList()
    {
        var result = await _handler.HandleAsync(new ListRefreshTokensCommand("user-1", "user-1"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task HandleAsync_AfterRevoke_IncludesInactiveToken()
    {
        var issued = (await _refreshTokens.IssueAsync("user-1")).Value!;
        await _refreshTokens.RevokeAsync(issued.Token);

        var result = await _handler.HandleAsync(new ListRefreshTokensCommand("user-1", "user-1"));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!, t => !t.IsActive);
    }
}
