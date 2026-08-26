namespace SanlamClaims.Domain.Interfaces;

public interface IClaimNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken);
}
