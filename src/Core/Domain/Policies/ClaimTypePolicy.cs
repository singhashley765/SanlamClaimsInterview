using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Domain.Policies;

public sealed record ClaimTypePolicy(ClaimType ClaimType, int SlaResolutionMinutes);
