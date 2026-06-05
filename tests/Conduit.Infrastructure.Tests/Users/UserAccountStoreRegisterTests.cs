using Conduit.Application.Common;
using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.Infrastructure.Tests.Users;

[Collection(nameof(Infrastructure.Tests.PostgresCollection))]
public sealed class UserAccountStoreRegisterTests(PostgresFixture fixture)
{
    [Fact]
    public async Task CreateAsync_ValidUser_ReturnsUserAccount()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var result = await store.CreateAsync(
            $"user_{suffix}",
            $"user_{suffix}@example.com",
            "jakejake");

        Assert.True(result.IsSuccess);
        Assert.Equal($"user_{suffix}", result.Value!.UserName);
        Assert.Equal($"user_{suffix}@example.com", result.Value.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Id));
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ReturnsConflict()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup_email_{suffix}@example.com";

        await store.CreateAsync($"first_{suffix}", email, "jakejake");

        var result = await store.CreateAsync($"second_{suffix}", email, "jakejake");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("has already been taken", result.Errors["email"]);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ReturnsConflict()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"dup_user_{suffix}";

        await store.CreateAsync(userName, $"one_{suffix}@example.com", "jakejake");

        var result = await store.CreateAsync(userName, $"two_{suffix}@example.com", "jakejake");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("has already been taken", result.Errors["username"]);
    }

    [Fact]
    public async Task CreateAsync_PasswordTooShort_ReturnsValidation()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var result = await store.CreateAsync(
            $"short_pw_{suffix}",
            $"short_pw_{suffix}@example.com",
            "12345");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.True(result.Errors.ContainsKey("password"));
    }

    [Fact]
    public async Task CreateAsync_PersistedUser_CanBeFoundByEmail()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"findable_{suffix}@example.com";

        var createResult = await store.CreateAsync($"findable_{suffix}", email, "jakejake");

        var found = await store.FindByEmailAsync(email);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(found);
        Assert.Equal(createResult.Value!.Id, found!.Id);
    }
}
