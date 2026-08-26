namespace SanlamClaims.Application.Common.Interfaces;

public sealed record PaymentRequest(
    string IdempotencyKey,
    string ClaimNumber,
    string PayeeName,
    string PayeeIdNumber,
    decimal Amount);
