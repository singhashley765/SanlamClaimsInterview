using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Application.Claims.DTOs;

public sealed record ClaimResponse(
    Guid Id,
    string ClaimNumber,
    ClaimType ClaimType,
    ClaimStatus Status,
    string FirstNames,
    string Surname,
    string IdNumber,
    string CellphoneNumber,
    string EmailAddress,
    string? Message,
    string PolicyNumber,
    decimal CoverageAmount,
    DateTime ResolutionDueAt,
    bool IsSlaBreached,
    bool IsPossibleDuplicate,
    Guid? DuplicateOfClaimId,
    decimal? ApprovedAmount,
    string? AssessmentNotes,
    string? AssessedBy,
    DateTime? AssessedAt,
    string? PaymentReference,
    DateTime? PaymentInitiatedAt,
    DateTime? PaymentCompletedAt,
    string? PaymentFailureReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);
