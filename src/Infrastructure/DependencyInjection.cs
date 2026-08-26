using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Interfaces;
using SanlamClaims.Infrastructure.ExternalClients;
using SanlamClaims.Infrastructure.Messaging.ServiceBus;
using SanlamClaims.Infrastructure.Persistence;
using SanlamClaims.Infrastructure.Persistence.Repositories;

namespace SanlamClaims.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClaimsDb")
            ?? throw new InvalidOperationException("ConnectionStrings:ClaimsDb is not configured.");

        services.AddDbContext<ClaimsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();

        var serviceBusConnectionString = configuration[$"{ServiceBusOptions.SectionName}:ConnectionString"]
            ?? throw new InvalidOperationException($"{ServiceBusOptions.SectionName}:ConnectionString is not configured.");

        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
        services.AddSingleton<IPaymentRequestPublisher, ServiceBusPaymentRequestPublisher>();
        services.AddHostedService<ServiceBusPaymentRequestConsumer>();

        var externalSystems = configuration.GetSection(ExternalSystemsOptions.SectionName).Get<ExternalSystemsOptions>()
            ?? throw new InvalidOperationException($"{ExternalSystemsOptions.SectionName} configuration section is missing.");

        services.AddHttpClient<IClientRegistryClient, ClientRegistryClient>(client =>
            {
                client.BaseAddress = new Uri(externalSystems.ClientRegistryBaseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        services.AddHttpClient<IPolicyManagementClient, PolicyManagementClient>(client =>
            {
                client.BaseAddress = new Uri(externalSystems.PolicyManagementBaseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
            {
                client.BaseAddress = new Uri(externalSystems.PaymentBaseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    private static void ConfigureResilience(Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions options)
    {
        // Payment calls are safe to retry too — every call carries an idempotency key.
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);

        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 4;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
    }
}
