using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Application.Claims.DTOs;

public sealed record SubmitClaimRequest(
    SubmissionChannel Application,
    ClaimType ClaimType,
    string FirstNames,
    string Surname,
    string IdNumber,
    string PolicyNumber,
    string CellphoneNumber,
    string EmailAddress,
    string? Message);
