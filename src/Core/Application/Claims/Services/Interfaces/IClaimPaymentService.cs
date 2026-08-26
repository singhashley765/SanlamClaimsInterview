using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Application.Claims.Services.Interfaces;

public interface IClaimPaymentService
{
    Task<Claim> RequestPaymentAsync(Guid claimId, CancellationToken cancellationToken);

    Task<Claim> ProcessPaymentAsync(Guid claimId, CancellationToken cancellationToken);
}
