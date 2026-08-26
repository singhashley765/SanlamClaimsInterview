namespace SanlamClaims.Infrastructure.ExternalClients;

public class ExternalSystemsOptions
{
    public const string SectionName = "ExternalSystems";

    public required string ClientRegistryBaseUrl { get; init; }

    public required string PolicyManagementBaseUrl { get; init; }

    public required string PaymentBaseUrl { get; init; }
}
