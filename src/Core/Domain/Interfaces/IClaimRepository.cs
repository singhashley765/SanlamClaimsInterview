using SanlamClaims.Domain.Common;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Domain.Interfaces;

public interface IClaimRepository
{
    Task AddAsync(Claim claim, CancellationToken cancellationToken);

    Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid?> FindAssessedDuplicateAsync(string idNumber, string policyNumber, ClaimType claimType, CancellationToken cancellationToken);

    Task<PagedResult<Claim>> GetAsync(
        ClaimStatus? status,
        ClaimType? claimType,
        bool? slaBreachedOnly,
        bool? possibleDuplicatesOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
