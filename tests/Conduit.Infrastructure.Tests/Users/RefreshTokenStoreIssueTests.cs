using Conduit.Application.Users;
using Conduit.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.Infrastructure.Tests.Users;

[Collection(nameof(Infrastructure.Tests.PostgresCollection))]
public sealed class RefreshTokenStoreIssueTests(PostgresFixture fixture)
{
    [Fact]
    public async Task IssueAsync_NewUser_CreatesActiveRefreshToken()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"refresh_{suffix}",
            $"refresh_{suffix}@example.com",
            "jakejake")).Value!;

        var result = await refreshTokens.IssueAsync(account.Id);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Token));
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task IssueAsync_CalledTwice_IssuesDistinctPerSessionTokens()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"reuse_{suffix}",
            $"reuse_{suffix}@example.com",
            "jakejake")).Value!;

        var first = await refreshTokens.IssueAsync(account.Id);
        var second = await refreshTokens.IssueAsync(account.Id);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value!.Token, second.Value!.Token);
    }

    [Fact]
    public async Task FindByTokenAsync_KnownToken_ReturnsTokenInfo()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"find_{suffix}",
            $"find_{suffix}@example.com",
            "jakejake")).Value!;
        var issued = (await refreshTokens.IssueAsync(account.Id)).Value!;

        var found = await refreshTokens.FindByTokenAsync(issued.Token);

        Assert.NotNull(found);
        Assert.Equal(account.Id, found!.UserId);
    }

    [Fact]
    public async Task FindByTokenAsync_UnknownToken_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

        var found = await refreshTokens.FindByTokenAsync("missing-token");

        Assert.Null(found);
    }

    [Fact]
    public async Task ListForUserAsync_AfterIssue_ReturnsTokens()
    {
        await using var scope = fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var account = (await users.CreateAsync(
            $"list_{suffix}",
            $"list_{suffix}@example.com",
            "jakejake")).Value!;
        await refreshTokens.IssueAsync(account.Id);

        var tokens = await refreshTokens.ListForUserAsync(account.Id);

        Assert.NotEmpty(tokens);
    }
}
