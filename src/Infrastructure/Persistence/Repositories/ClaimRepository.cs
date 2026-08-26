using Microsoft.EntityFrameworkCore;
using SanlamClaims.Domain.Common;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Infrastructure.Persistence.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _context;

    public ClaimRepository(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken)
    {
        await _context.Claims.AddAsync(claim, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Claims
            .Include(c => c.StatusHistory)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Guid?> FindAssessedDuplicateAsync(string idNumber, string policyNumber, ClaimType claimType, CancellationToken cancellationToken) =>
        _context.Claims
            .Where(c => c.IdNumber == idNumber && c.PolicyNumber == policyNumber && c.ClaimType == claimType && c.AssessedAt != null)
            .OrderByDescending(c => c.AssessedAt)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Claim>> GetAsync(
        ClaimStatus? status,
        ClaimType? claimType,
        bool? slaBreachedOnly,
        bool? possibleDuplicatesOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Claims.AsQueryable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (claimType is not null)
        {
            query = query.Where(c => c.ClaimType == claimType);
        }

        if (possibleDuplicatesOnly is true)
        {
            query = query.Where(c => c.IsPossibleDuplicate);
        }

        if (slaBreachedOnly is true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(c =>
                (c.AssessedAt.HasValue && c.AssessedAt.Value > c.ResolutionDueAt) ||
                (!c.AssessedAt.HasValue && now > c.ResolutionDueAt));
        }

        // Most SLA-urgent first — this is what makes the list work as an analyst inbox.
        query = query.OrderBy(c => c.ResolutionDueAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, page, pageSize, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
