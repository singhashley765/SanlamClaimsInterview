namespace SanlamClaims.Domain.Exceptions;

public class ClaimNotFoundException : DomainException
{
    public ClaimNotFoundException(Guid claimId)
        : base($"Claim '{claimId}' was not found.")
    {
        ClaimId = claimId;
    }

    public Guid ClaimId { get; }
}
