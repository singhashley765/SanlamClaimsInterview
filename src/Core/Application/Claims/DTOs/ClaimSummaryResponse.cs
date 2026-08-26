using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Application.Claims.DTOs;

public sealed record ClaimSummaryResponse(
    Guid Id,
    string ClaimNumber,
    ClaimType ClaimType,
    ClaimStatus Status,
    string FirstNames,
    string Surname,
    DateTime ResolutionDueAt,
    bool IsSlaBreached,
    bool IsPossibleDuplicate,
    Guid? DuplicateOfClaimId);
