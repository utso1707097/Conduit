using System.Security.Cryptography;
using System.Text;
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
        // Per-session token: every login/registration gets its own refresh token
        // (its own family) so individual sessions/devices can be revoked independently.
        var (entity, plaintext) = CreateEntity(userId, familyId: Guid.NewGuid());
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RefreshTokenInfo>.Ok(ToInfo(entity, plaintext));
    }

    public async Task<RefreshTokenInfo?> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        var entity = await db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Echo the caller-supplied plaintext: they already hold it, so this leaks nothing.
        return entity is null ? null : ToInfo(entity, token);
    }

    public async Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        var entity = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (entity is null)
        {
            return Result.NotFound("refreshToken", "was not found");
        }

        if (!IsActive(entity))
        {
            return Result.Validation("refreshToken", "is not active");
        }

        entity.RevokedUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<RefreshTokenInfo>> RotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        var entity = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (entity is null)
        {
            return Result<RefreshTokenInfo>.Unauthorized("refreshToken", "did not match any user");
        }

        // Reuse detection: a token that exists but is no longer active was already
        // rotated or revoked. Replaying it signals theft, so revoke the whole family.
        if (!IsActive(entity))
        {
            await RevokeFamilyAsync(entity.FamilyId, cancellationToken);
            return Result<RefreshTokenInfo>.Unauthorized("refreshToken", "has been revoked");
        }

        entity.RevokedUtc = clock.GetUtcNow().UtcDateTime;
        var (replacement, plaintext) = CreateEntity(entity.UserId, entity.FamilyId);
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RefreshTokenInfo>.Ok(ToInfo(replacement, plaintext));
    }

    public async Task<IReadOnlyList<RefreshTokenSummary>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var tokens = await db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedUtc)
            .ToListAsync(cancellationToken);

        return tokens
            .Select(t => new RefreshTokenSummary(
                t.CreatedUtc,
                t.ExpiresUtc,
                t.RevokedUtc,
                t.RevokedUtc is null && t.ExpiresUtc > now))
            .ToList();
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedUtc == null && t.ExpiresUtc > now)
            .ToListAsync(cancellationToken);

        if (active.Count == 0)
        {
            return;
        }

        foreach (var token in active)
        {
            token.RevokedUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private (RefreshToken Entity, string Plaintext) CreateEntity(string userId, Guid familyId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var plaintext = GenerateToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = userId,
            TokenHash = Hash(plaintext),
            CreatedUtc = now,
            ExpiresUtc = now.AddDays(jwtOptions.Value.RefreshTokenDurationInDays)
        };

        return (entity, plaintext);
    }

    private bool IsActive(RefreshToken entity) =>
        entity.RevokedUtc is null && entity.ExpiresUtc > clock.GetUtcNow().UtcDateTime;

    private static RefreshTokenInfo ToInfo(RefreshToken entity, string plaintext) =>
        new(entity.UserId, plaintext, entity.ExpiresUtc, entity.CreatedUtc, entity.RevokedUtc);

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
