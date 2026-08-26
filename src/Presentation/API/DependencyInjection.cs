using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SanlamClaims.API.Auth;
using SanlamClaims.API.Common;
using SanlamClaims.API.RateLimiting;
using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddOpenApi();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
        services.AddTransient<CorrelationPropagationHandler>();
        services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler<CorrelationPropagationHandler>());

        return services;
    }

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException("Auth configuration section is missing.");

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton<TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.JwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey)),
                };
            });
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("public", httpContext => RateLimitPartition.GetSlidingWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitingOptions.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0,
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests. Please try again shortly.",
                    },
                    cancellationToken);
            };
        });

        return services;
    }
}
