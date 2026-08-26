namespace SanlamClaims.API.Auth;

public sealed record LoginResponse(string Token, DateTime ExpiresAt);
