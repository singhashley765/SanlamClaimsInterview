using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Domain.Exceptions;

public class InvalidClaimStateTransitionException : DomainException
{
    public InvalidClaimStateTransitionException(ClaimStatus from, ClaimStatus to)
        : base($"Cannot transition claim from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }

    public ClaimStatus From { get; }

    public ClaimStatus To { get; }
}
