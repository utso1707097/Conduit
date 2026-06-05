using Conduit.Application.Common;
using Xunit;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Users;

public sealed class LoginUserHandlerTests
{
    private readonly FakeUserAccountStore _store = new();
    private readonly FakeTokenIssuer _tokens = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(_store, _tokens, _refreshTokens);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsAuthenticatedUserWithToken()
    {
        await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        var command = new LoginUserCommand("jake@jake.jake", "jakejake");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("jake", result.Value!.Account.UserName);
        Assert.StartsWith("test-token-for-", result.Value.Token);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken.Token));
    }

    [Fact]
    public async Task HandleAsync_BlankEmail_ReturnsValidationError()
    {
        var command = new LoginUserCommand("", "jakejake");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["email"]);
    }

    [Fact]
    public async Task HandleAsync_BlankPassword_ReturnsValidationError()
    {
        var command = new LoginUserCommand("jake@jake.jake", "");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["password"]);
    }

    [Fact]
    public async Task HandleAsync_InvalidCredentials_ReturnsUnauthorized()
    {
        await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        var command = new LoginUserCommand("jake@jake.jake", "wrong-password");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
        Assert.Contains("is invalid", result.Errors["email or password"]);
    }

    [Fact]
    public async Task HandleAsync_EmailWithSurroundingWhitespace_TrimsBeforeLookup()
    {
        await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        var command = new LoginUserCommand("  jake@jake.jake  ", "jakejake");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("jake@jake.jake", result.Value!.Account.Email);
    }
}
