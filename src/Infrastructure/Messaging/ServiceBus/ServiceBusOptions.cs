namespace SanlamClaims.Infrastructure.Messaging.ServiceBus;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string? ConnectionString { get; init; }

    public string QueueName { get; init; } = "payment-requests";
}
