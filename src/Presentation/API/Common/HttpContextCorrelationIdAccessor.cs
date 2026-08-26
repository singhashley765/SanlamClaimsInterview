using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.API.Common;

public class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString() is { Length: > 0 } value
            ? value
            : null;
}
