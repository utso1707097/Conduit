using Conduit.Application.Common;
using Xunit;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Users;

public sealed class RegisterUserHandlerTests
{
    private readonly FakeUserAccountStore _store = new();
    private readonly FakeTokenIssuer _tokens = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_store, _tokens, _refreshTokens);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsAuthenticatedUserWithToken()
    {
        var command = new RegisterUserCommand { UserName = "jake", Email = "jake@jake.jake", Password = "jakejake" };

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("jake", result.Value!.Account.UserName);
        Assert.Equal("jake@jake.jake", result.Value.Account.Email);
        Assert.StartsWith("test-token-for-", result.Value.Token);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken.Token));
    }

    [Fact]
    public async Task HandleAsync_BlankUsername_ReturnsValidationError()
    {
        var command = new RegisterUserCommand { UserName = "  ", Email = "jake@jake.jake", Password = "jakejake" };

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["username"]);
    }

    [Fact]
    public async Task HandleAsync_BlankEmail_ReturnsValidationError()
    {
        var command = new RegisterUserCommand { UserName = "jake", Email = "", Password = "jakejake" };

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["email"]);
    }

    [Fact]
    public async Task HandleAsync_BlankPassword_ReturnsValidationError()
    {
        var command = new RegisterUserCommand { UserName = "jake", Email = "jake@jake.jake", Password = "   " };

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["password"]);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsConflictFromStore()
    {
        _store.CreateHandler = (_, _, _) =>
            Result<UserAccount>.Conflict("email", "has already been taken");

        var command = new RegisterUserCommand { UserName = "jake", Email = "taken@jake.jake", Password = "jakejake" };

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("has already been taken", result.Errors["email"]);
    }
}
