namespace SanlamClaims.API.Common;

public class CorrelationPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
