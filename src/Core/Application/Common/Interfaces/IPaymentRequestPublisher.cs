namespace SanlamClaims.Application.Common.Interfaces;

public interface IPaymentRequestPublisher
{
    Task PublishAsync(PaymentRequestedMessage message, CancellationToken cancellationToken);
}
