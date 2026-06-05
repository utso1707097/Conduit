namespace Conduit.Application.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "JWT";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int DurationInMinutes { get; set; } = 15;

    public int RefreshTokenDurationInDays { get; set; } = 10;
}
