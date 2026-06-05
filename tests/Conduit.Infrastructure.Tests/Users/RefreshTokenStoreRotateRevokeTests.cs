using Conduit.Application.Common;
using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.Infrastructure.Tests.Users;

[Collection(nameof(Infrastructure.Tests.PostgresCollection))]
public sealed class RefreshTokenStoreRotateRevokeTests(PostgresFixture fixture)
{
    [Fact]
    public async Task RotateAsync_ValidToken_RevokesOldAndReturnsNewToken()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"rotate_{suffix}",
            $"rotate_{suffix}@example.com",
            "jakejake")).Value!;
        var issued = (await refreshTokens.IssueAsync(account.Id)).Value!;

        var rotated = await refreshTokens.RotateAsync(issued.Token);

        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(issued.Token, rotated.Value!.Token);
        var old = await refreshTokens.FindByTokenAsync(issued.Token);
        Assert.NotNull(old);
        Assert.False(old!.IsActive);
    }

    [Fact]
    public async Task RotateAsync_UnknownToken_ReturnsUnauthorized()
    {
        await using var scope = fixture.CreateScope();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

        var result = await refreshTokens.RotateAsync("missing-token");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task RevokeAsync_ValidToken_MarksTokenInactive()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"revoke_{suffix}",
            $"revoke_{suffix}@example.com",
            "jakejake")).Value!;
        var issued = (await refreshTokens.IssueAsync(account.Id)).Value!;

        var result = await refreshTokens.RevokeAsync(issued.Token);

        Assert.True(result.IsSuccess);
        var found = await refreshTokens.FindByTokenAsync(issued.Token);
        Assert.NotNull(found);
        Assert.False(found!.IsActive);
    }

    [Fact]
    public async Task RevokeAsync_UnknownToken_ReturnsNotFound()
    {
        await using var scope = fixture.CreateScope();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

        var result = await refreshTokens.RevokeAsync("missing-token");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task RotateAsync_AlreadyRevokedToken_ReturnsUnauthorized()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"inactive_{suffix}",
            $"inactive_{suffix}@example.com",
            "jakejake")).Value!;
        var issued = (await refreshTokens.IssueAsync(account.Id)).Value!;
        await refreshTokens.RevokeAsync(issued.Token);

        var result = await refreshTokens.RotateAsync(issued.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }
}
