using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Infrastructure.ExternalClients;

public class ClientRegistryClient : IClientRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientRegistryClient> _logger;

    public ClientRegistryClient(HttpClient httpClient, ILogger<ClientRegistryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ClientDetails?> GetClientByIdNumberAsync(string idNumber, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/clients/by-id-number/{Uri.EscapeDataString(idNumber)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClientDetails>(JsonDefaults.Options, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or BrokenCircuitException)
        {
            _logger.LogError(ex, "Client Registry lookup failed for ID number {IdNumber}", idNumber);
            throw new ExternalSystemException("ClientRegistry", $"Client Registry lookup failed for ID number '{idNumber}'.", ex);
        }
    }
}
