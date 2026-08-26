namespace SanlamClaims.Application.Common.Interfaces;

public sealed record PaymentRequestedMessage(Guid ClaimId, string? CorrelationId);
