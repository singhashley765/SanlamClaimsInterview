using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Application.Claims.Services.Interfaces;

public interface IClaimSubmissionService
{
    Task<Claim> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken);
}
