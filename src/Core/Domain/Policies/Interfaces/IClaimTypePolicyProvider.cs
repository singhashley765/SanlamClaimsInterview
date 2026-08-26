using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Policies;

namespace SanlamClaims.Domain.Policies.Interfaces;

public interface IClaimTypePolicyProvider
{
    ClaimTypePolicy GetPolicy(ClaimType claimType);
}
