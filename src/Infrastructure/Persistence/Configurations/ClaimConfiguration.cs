using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(c => c.ClaimNumber).IsUnique();

        builder.Property(c => c.FirstNames).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Surname).IsRequired().HasMaxLength(128);
        builder.Property(c => c.IdNumber).IsRequired().HasMaxLength(13);
        builder.Property(c => c.CellphoneNumber).IsRequired().HasMaxLength(16);
        builder.Property(c => c.EmailAddress).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Message).HasMaxLength(2000);

        builder.Property(c => c.ClientFullName).IsRequired().HasMaxLength(256);

        builder.Property(c => c.PolicyNumber).IsRequired().HasMaxLength(64);
        builder.Property(c => c.CoverageAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ApprovedAmount).HasColumnType("decimal(18,2)");

        builder.Property(c => c.AssessmentNotes).HasMaxLength(2000);
        builder.Property(c => c.AssessedBy).HasMaxLength(128);

        builder.Property(c => c.PaymentReference).HasMaxLength(64);
        builder.Property(c => c.PaymentFailureReason).HasMaxLength(500);

        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasIndex(c => new { c.IdNumber, c.PolicyNumber, c.ClaimType });

        // IsSlaBreached is computed from other properties at read time, not a stored column.
        builder.Ignore(c => c.IsSlaBreached);

        builder.HasMany(c => c.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
