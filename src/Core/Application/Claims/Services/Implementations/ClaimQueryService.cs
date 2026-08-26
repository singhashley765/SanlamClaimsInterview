using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Domain.Common;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Application.Claims.Services.Implementations;

public class ClaimQueryService : IClaimQueryService
{
    private const int MaxPageSize = 100;

    private readonly IClaimRepository _claimRepository;

    public ClaimQueryService(IClaimRepository claimRepository)
    {
        _claimRepository = claimRepository;
    }

    public async Task<Claim> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _claimRepository.GetByIdAsync(id, cancellationToken) ?? throw new ClaimNotFoundException(id);

    public Task<PagedResult<Claim>> GetAsync(
        ClaimStatus? status,
        ClaimType? claimType,
        bool? slaBreachedOnly,
        bool? possibleDuplicatesOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // default if page is out of range
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        return _claimRepository.GetAsync(status, claimType, slaBreachedOnly, possibleDuplicatesOnly, normalizedPage, normalizedPageSize, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClaimStatusHistory>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var claim = await GetByIdAsync(id, cancellationToken);
        return claim.StatusHistory.OrderBy(h => h.ChangedAt).ToList();
    }
}
