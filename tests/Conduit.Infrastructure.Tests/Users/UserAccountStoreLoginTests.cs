using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.Infrastructure.Tests.Users;

[Collection(nameof(Infrastructure.Tests.PostgresCollection))]
public sealed class UserAccountStoreLoginTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsAccount()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_ok_{suffix}@example.com";
        await store.CreateAsync($"login_ok_{suffix}", email, "jakejake");

        var account = await store.ValidateCredentialsAsync(email, "jakejake");

        Assert.NotNull(account);
        Assert.Equal(email, account!.Email);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WrongPassword_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_bad_pw_{suffix}@example.com";
        await store.CreateAsync($"login_bad_pw_{suffix}", email, "jakejake");

        var account = await store.ValidateCredentialsAsync(email, "not-the-password");

        Assert.Null(account);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UnknownEmail_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();

        var account = await store.ValidateCredentialsAsync(
            $"missing_{Guid.NewGuid():N}@example.com",
            "jakejake");

        Assert.Null(account);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_EmptyPassword_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_empty_pw_{suffix}@example.com";
        await store.CreateAsync($"login_empty_pw_{suffix}", email, "jakejake");

        var account = await store.ValidateCredentialsAsync(email, "");

        Assert.Null(account);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ExceedsFailedAttempts_LocksAccount()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"lockout_{suffix}@example.com";
        await store.CreateAsync($"lockout_{suffix}", email, "jakejake");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await store.ValidateCredentialsAsync(email, "wrong-password");
        }

        // Even the correct password is rejected once the account is locked out.
        var afterLockout = await store.ValidateCredentialsAsync(email, "jakejake");

        Assert.Null(afterLockout);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_AfterRegister_WorksWithSamePassword()
    {
        await using var scope = fixture.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"roundtrip_{suffix}";
        var email = $"roundtrip_{suffix}@example.com";
        const string password = "jakejake";

        var created = await store.CreateAsync(userName, email, password);
        var validated = await store.ValidateCredentialsAsync(email, password);

        Assert.True(created.IsSuccess);
        Assert.NotNull(validated);
        Assert.Equal(userName, validated!.UserName);
    }
}
