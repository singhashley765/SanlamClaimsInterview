namespace SanlamClaims.Application.Claims.DTOs;

public enum AssessmentDecision
{
    Approve,
    Reject,
}

public sealed record AssessClaimRequest(
    AssessmentDecision Decision,
    decimal? ApprovedAmount,
    string Notes,
    string AssessedBy);
