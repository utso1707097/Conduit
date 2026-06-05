using System.Security.Cryptography;

namespace Conduit.Infrastructure.Auth;

public static class RefreshTokenGenerator
{
    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
