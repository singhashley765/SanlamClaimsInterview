using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Infrastructure.ExternalClients;

public class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentClient> _logger;

    public PaymentClient(HttpClient httpClient, ILogger<PaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(request, options: JsonDefaults.Options),
            };
            httpRequest.Headers.Add("Idempotency-Key", request.IdempotencyKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            // 422 = business decline (e.g. insufficient funds), not a transport failure.
            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.UnprocessableEntity)
            {
                var result = await response.Content.ReadFromJsonAsync<PaymentResult>(JsonDefaults.Options, cancellationToken);
                return result ?? new PaymentResult(false, null, "Payment system returned an empty response.");
            }

            response.EnsureSuccessStatusCode();
            return new PaymentResult(false, null, $"Unexpected payment system response: {response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or BrokenCircuitException)
        {
            _logger.LogError(ex, "Payment system call failed for claim {ClaimNumber}", request.ClaimNumber);
            throw new ExternalSystemException("Payment", $"Payment system call failed for claim '{request.ClaimNumber}'.", ex);
        }
    }
}
