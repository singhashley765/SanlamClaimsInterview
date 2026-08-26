using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Infrastructure.Messaging.ServiceBus;

public sealed class ServiceBusPaymentRequestConsumer : BackgroundService
{
    private readonly ServiceBusSessionProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusPaymentRequestConsumer> _logger;

    public ServiceBusPaymentRequestConsumer(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusPaymentRequestConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _processor = client.CreateSessionProcessor(
            options.Value.QueueName,
            new ServiceBusSessionProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentSessions = 4,
                MaxConcurrentCallsPerSession = 1,
            });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _processor.StartProcessingAsync(stoppingToken);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
        await _processor.DisposeAsync();
    }

    private async Task HandleMessageAsync(ProcessSessionMessageEventArgs args)
    {
        var message = JsonSerializer.Deserialize<PaymentRequestedMessage>(args.Message.Body);
        if (message is null)
        {
            // Malformed message — dead-letter it
            await args.DeadLetterMessageAsync(args.Message, "MalformedBody", cancellationToken: args.CancellationToken);
            return;
        }

        try
        {
            // Scoped services need their own scope per message.
            using var scope = _scopeFactory.CreateScope();
            using var correlationScope = _logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = message.CorrelationId });
            var paymentService = scope.ServiceProvider.GetRequiredService<IClaimPaymentService>();
            await paymentService.ProcessPaymentAsync(message.ClaimId, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure processing payment request for claim {ClaimId}; abandoning for redelivery", message.ClaimId);

            // Abandon puts it back on the queue; MaxDeliveryCount decides when to dead-letter.
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error in {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }
}
