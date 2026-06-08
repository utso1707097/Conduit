namespace Conduit.Api.Infrastructure;

public static class RateLimiterPolicies
{
    // Throttles authentication endpoints (register/login/refresh/revoke) to blunt
    // credential-stuffing and brute-force attempts.
    public const string Auth = "auth";
}
