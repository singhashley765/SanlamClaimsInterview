using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Application.Claims.Mapping;

public static class ClaimMapper
{
    public static ClaimResponse ToResponse(Claim claim) => new ClaimResponse(
        claim.Id,
        claim.ClaimNumber,
        claim.ClaimType,
        claim.Status,
        claim.FirstNames,
        claim.Surname,
        claim.IdNumber,
        claim.CellphoneNumber,
        claim.EmailAddress,
        claim.Message,
        claim.PolicyNumber,
        claim.CoverageAmount,
        claim.ResolutionDueAt,
        claim.IsSlaBreached,
        claim.IsPossibleDuplicate,
        claim.DuplicateOfClaimId,
        claim.ApprovedAmount,
        claim.AssessmentNotes,
        claim.AssessedBy,
        claim.AssessedAt,
        claim.PaymentReference,
        claim.PaymentInitiatedAt,
        claim.PaymentCompletedAt,
        claim.PaymentFailureReason,
        claim.CreatedAt,
        claim.UpdatedAt);

    public static ClaimSummaryResponse ToSummary(Claim claim) => new ClaimSummaryResponse(
        claim.Id,
        claim.ClaimNumber,
        claim.ClaimType,
        claim.Status,
        claim.FirstNames,
        claim.Surname,
        claim.ResolutionDueAt,
        claim.IsSlaBreached,
        claim.IsPossibleDuplicate,
        claim.DuplicateOfClaimId);

    public static ClaimHistoryEntryResponse ToHistoryEntry(ClaimStatusHistory entry) => new ClaimHistoryEntryResponse(
        entry.FromStatus,
        entry.ToStatus,
        entry.ChangedBy,
        entry.Reason,
        entry.ChangedAt);
}
