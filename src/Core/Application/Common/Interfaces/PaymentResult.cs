namespace SanlamClaims.Application.Common.Interfaces;

public sealed record PaymentResult(bool Success, string? PaymentReference, string? FailureReason);
