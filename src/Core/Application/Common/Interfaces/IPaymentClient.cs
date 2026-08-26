namespace SanlamClaims.Application.Common.Interfaces;

public interface IPaymentClient
{
    Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
}
