namespace SanlamClaims.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;
}
