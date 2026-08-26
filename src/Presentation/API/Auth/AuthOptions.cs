namespace SanlamClaims.API.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string JwtSigningKey { get; init; } = string.Empty;

    public string JwtIssuer { get; init; } = string.Empty;

    public string JwtAudience { get; init; } = string.Empty;

    public int JwtExpiryMinutes { get; init; } = 60;
}
