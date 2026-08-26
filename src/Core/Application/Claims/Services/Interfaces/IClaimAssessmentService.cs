using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Domain.Entities;

namespace SanlamClaims.Application.Claims.Services.Interfaces;

public interface IClaimAssessmentService
{
    Task<Claim> AssessAsync(Guid claimId, AssessClaimRequest request, CancellationToken cancellationToken);
}
