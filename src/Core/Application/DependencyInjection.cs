using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SanlamClaims.Application.Claims.Services.Implementations;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Application.Common;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Policies.Implementations;
using SanlamClaims.Domain.Policies.Interfaces;

namespace SanlamClaims.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IClaimTypePolicyProvider, ClaimTypePolicyProvider>();

        services.AddScoped<IClaimSubmissionService, ClaimSubmissionService>();
        services.AddScoped<IClaimAssessmentService, ClaimAssessmentService>();
        services.AddScoped<IClaimPaymentService, ClaimPaymentService>();
        services.AddScoped<IClaimQueryService, ClaimQueryService>();

        return services;
    }
}
