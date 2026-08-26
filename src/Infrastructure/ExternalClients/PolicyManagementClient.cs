using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Infrastructure.ExternalClients;

public class PolicyManagementClient : IPolicyManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PolicyManagementClient> _logger;

    public PolicyManagementClient(HttpClient httpClient, ILogger<PolicyManagementClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/policies/{Uri.EscapeDataString(policyNumber)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PolicyDetails>(JsonDefaults.Options, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or BrokenCircuitException)
        {
            _logger.LogError(ex, "Policy Management lookup failed for policy {PolicyNumber}", policyNumber);
            throw new ExternalSystemException("PolicyManagement", $"Policy Management lookup failed for '{policyNumber}'.", ex);
        }
    }
}
