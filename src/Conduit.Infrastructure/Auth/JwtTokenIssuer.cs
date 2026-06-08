using System.Security.Claims;
using System.Text;
using Conduit.Application.Settings;
using Conduit.Application.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Infrastructure.Auth;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtSettings settings;
    private readonly TimeProvider clock;
    private readonly SigningCredentials credentials;
    private readonly JsonWebTokenHandler handler = new();

    public JwtTokenIssuer(IOptions<JwtSettings> jwtOptions, TimeProvider clock)
    {
        settings = jwtOptions.Value;
        this.clock = clock;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string IssueToken(UserAccount user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(settings.DurationInMinutes),
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ])
        };

        return handler.CreateToken(descriptor);
    }
}
