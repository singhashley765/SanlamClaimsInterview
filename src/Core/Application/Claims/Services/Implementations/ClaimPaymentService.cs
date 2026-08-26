using Microsoft.Extensions.Logging;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Application.Common;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Application.Claims.Services.Implementations;

public class ClaimPaymentService : IClaimPaymentService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IPaymentClient _paymentClient;
    private readonly IPaymentRequestPublisher _paymentRequestPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<ClaimPaymentService> _logger;

    public ClaimPaymentService(
        IClaimRepository claimRepository,
        IPaymentClient paymentClient,
        IPaymentRequestPublisher paymentRequestPublisher,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<ClaimPaymentService> logger)
    {
        _claimRepository = claimRepository;
        _paymentClient = paymentClient;
        _paymentRequestPublisher = paymentRequestPublisher;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task<Claim> RequestPaymentAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new ClaimNotFoundException(claimId);

        if (claim.Status is not (ClaimStatus.Approved or ClaimStatus.PaymentFailed))
        {
            throw new InvalidClaimStateTransitionException(claim.Status, ClaimStatus.PaymentInitiated);
        }

        await _paymentRequestPublisher.PublishAsync(new PaymentRequestedMessage(claim.Id, _correlationIdAccessor.CorrelationId), cancellationToken);

        _logger.LogInformation("Payment requested for claim {ClaimNumber}, queued for processing", claim.ClaimNumber);

        return claim;
    }

    public async Task<Claim> ProcessPaymentAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new ClaimNotFoundException(claimId);

        // claim.Id is a stable idempotency key, so a redelivered or retried request never pays twice.
        var idempotencyKey = claim.Id.ToString();

        claim.InitiatePayment(idempotencyKey, SystemActors.PaymentProcessor);
        await _claimRepository.SaveChangesAsync(cancellationToken);

        var request = new PaymentRequest(idempotencyKey, claim.ClaimNumber, claim.ClientFullName, claim.IdNumber, claim.ApprovedAmount!.Value);

        try
        {
            var result = await _paymentClient.InitiatePaymentAsync(request, cancellationToken);

            if (result.Success)
            {
                claim.CompletePayment(SystemActors.PaymentProcessor);
                _logger.LogInformation("Payment completed for claim {ClaimNumber} ({Amount:C})", claim.ClaimNumber, claim.ApprovedAmount);
            }
            else
            {
                claim.FailPayment(result.FailureReason ?? "Payment declined by the Payment system.", SystemActors.PaymentProcessor);
                _logger.LogWarning("Payment declined for claim {ClaimNumber}: {Reason}", claim.ClaimNumber, result.FailureReason);
            }
        }
        catch (ExternalSystemException ex)
        {
            claim.FailPayment($"Payment system unavailable: {ex.Message}", SystemActors.PaymentProcessor);
            _logger.LogError(ex, "Payment system unavailable while paying claim {ClaimNumber}", claim.ClaimNumber);
        }

        await _claimRepository.SaveChangesAsync(cancellationToken);
        return claim;
    }
}
