using SanlamClaims.Domain.Common;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Application.Claims.Services.Interfaces;

public interface IClaimQueryService
{
    Task<Claim> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Claim>> GetAsync(
        ClaimStatus? status,
        ClaimType? claimType,
        bool? slaBreachedOnly,
        bool? possibleDuplicatesOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ClaimStatusHistory>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}
