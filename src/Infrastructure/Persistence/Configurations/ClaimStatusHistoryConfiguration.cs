using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Infrastructure.Persistence.Configurations;

public class ClaimStatusHistoryConfiguration : IEntityTypeConfiguration<ClaimStatusHistory>
{
    public void Configure(EntityTypeBuilder<ClaimStatusHistory> builder)
    {
        builder.ToTable("ClaimStatusHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangedBy).IsRequired().HasMaxLength(128);
        builder.Property(h => h.Reason).HasMaxLength(2000);
    }
}
