using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Infrastructure.Messaging.ServiceBus;

public sealed class ServiceBusPaymentRequestPublisher : IPaymentRequestPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ServiceBusPaymentRequestPublisher(ServiceBusClient client, IOptions<ServiceBusOptions> options)
    {
        _sender = client.CreateSender(options.Value.QueueName);
    }

    public async Task PublishAsync(PaymentRequestedMessage message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var serviceBusMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            MessageId = message.ClaimId.ToString(),
        };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync() => await _sender.DisposeAsync();
}
