using Microsoft.EntityFrameworkCore;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Infrastructure.Persistence.Repositories;

/// <summary>Draws from a SQL Server sequence so claim numbers never collide under concurrent writes.</summary>
public class ClaimNumberGenerator : IClaimNumberGenerator
{
    private readonly ClaimsDbContext _context;

    public ClaimNumberGenerator(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        var sql = "SELECT NEXT VALUE FOR " + ClaimsDbContext.ClaimNumberSequenceName;
        var sequenceValue = await _context.Database.SqlQueryRaw<int>(sql).SingleAsync(cancellationToken);

        return $"CLM-{DateTime.UtcNow:yyyy}-{sequenceValue:D6}";
    }
}
