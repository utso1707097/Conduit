using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Conduit.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    // The JWT bearer pipeline is configured with MapInboundClaims = false and
    // NameClaimType = "sub", so the user id is carried verbatim in the "sub" claim.
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;
}
