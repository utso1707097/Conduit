using Conduit.Application.Common;
using Conduit.Application.Tests.Fakes;
using Conduit.Application.Users;
using Xunit;

namespace Conduit.Application.Tests.Users;

public sealed class UpdateCurrentUserHandlerTests
{
    private readonly FakeUserAccountStore _store = new();
    private readonly FakeTokenIssuer _tokens = new();
    private readonly UpdateCurrentUserHandler _handler;

    public UpdateCurrentUserHandlerTests() =>
        _handler = new UpdateCurrentUserHandler(_store, _tokens);

    [Fact]
    public async Task HandleAsync_UpdateBio_ReturnsUpdatedUserWithNewToken()
    {
        var created = await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        var command = new UpdateCurrentUserCommand { Bio = "I like to skateboard" };

        var result = await _handler.HandleAsync(
            new UpdateCurrentUserRequest(created.Value!.Id, command));

        Assert.True(result.IsSuccess);
        Assert.Equal("I like to skateboard", result.Value!.Account.Bio);
        Assert.StartsWith("test-token-for-", result.Value.Token);
    }

    [Fact]
    public async Task HandleAsync_EmptyBody_ReturnsValidationError()
    {
        var created = await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");

        var result = await _handler.HandleAsync(
            new UpdateCurrentUserRequest(created.Value!.Id, new UpdateCurrentUserCommand()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("at least one field", result.Errors["user"][0]);
    }

    [Fact]
    public async Task HandleAsync_BlankEmail_ReturnsValidationError()
    {
        var created = await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        var command = new UpdateCurrentUserCommand { Email = "  " };

        var result = await _handler.HandleAsync(
            new UpdateCurrentUserRequest(created.Value!.Id, command));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Contains("can't be blank", result.Errors["email"]);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsConflict()
    {
        var first = await _store.CreateAsync("jake", "jake@jake.jake", "jakejake");
        await _store.CreateAsync("other", "other@example.com", "jakejake");
        var command = new UpdateCurrentUserCommand { Email = "other@example.com" };

        var result = await _handler.HandleAsync(
            new UpdateCurrentUserRequest(first.Value!.Id, command));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("has already been taken", result.Errors["email"]);
    }
}
