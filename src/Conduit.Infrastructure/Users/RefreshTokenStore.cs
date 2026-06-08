using System.Security.Cryptography;
using Conduit.Application.Common;
using Conduit.Application.Settings;
using Conduit.Application.Users;
using Conduit.Infrastructure.Models;
using Conduit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Conduit.Infrastructure.Users;

public sealed class RefreshTokenStore(
    ConduitDbContext db,
    IOptions<JwtSettings> jwtOptions,
    TimeProvider clock) : IRefreshTokenStore
{
    public async Task<Result<RefreshTokenInfo>> IssueAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var active = await db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > clock.GetUtcNow().UtcDateTime)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (active is not null)
        {
            return Result<RefreshTokenInfo>.Ok(ToInfo(active));
        }

        var entity = CreateEntity(userId);
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RefreshTokenInfo>.Ok(ToInfo(entity));
    }

    public async Task<RefreshTokenInfo?> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        return entity is null ? null : ToInfo(entity);
    }

    public async Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (entity is null)
        {
            return Result.NotFound("refreshToken", "was not found");
        }

        if (!IsActive(entity))
        {
            return Result.Fail(
                ErrorKind.Validation,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["is not active"]
                });
        }

        entity.RevokedUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<RefreshTokenInfo>> RotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (entity is null)
        {
            return Result<RefreshTokenInfo>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["did not match any user"]
                });
        }

        if (!IsActive(entity))
        {
            return Result<RefreshTokenInfo>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["is not active"]
                });
        }

        entity.RevokedUtc = clock.GetUtcNow().UtcDateTime;
        var replacement = CreateEntity(entity.UserId);
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RefreshTokenInfo>.Ok(ToInfo(replacement));
    }

    public async Task<IReadOnlyList<RefreshTokenInfo>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedUtc)
            .ToListAsync(cancellationToken);

        return tokens.Select(ToInfo).ToList();
    }

    private RefreshToken CreateEntity(string userId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateToken(),
            CreatedUtc = now,
            ExpiresUtc = now.AddDays(jwtOptions.Value.RefreshTokenDurationInDays)
        };
    }

    private bool IsActive(RefreshToken entity) =>
        entity.RevokedUtc is null && entity.ExpiresUtc > clock.GetUtcNow().UtcDateTime;

    private static RefreshTokenInfo ToInfo(RefreshToken entity) =>
        new(entity.UserId, entity.Token, entity.ExpiresUtc, entity.CreatedUtc, entity.RevokedUtc);

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
