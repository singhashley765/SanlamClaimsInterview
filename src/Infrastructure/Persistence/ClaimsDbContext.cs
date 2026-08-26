using Microsoft.EntityFrameworkCore;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Infrastructure.Persistence;

public class ClaimsDbContext : DbContext
{
    public const string ClaimNumberSequenceName = "ClaimNumberSequence";

    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<ClaimStatusHistory> ClaimStatusHistories => Set<ClaimStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
        modelBuilder.HasSequence<int>(ClaimNumberSequenceName).StartsAt(1).IncrementsBy(1);
    }
}
