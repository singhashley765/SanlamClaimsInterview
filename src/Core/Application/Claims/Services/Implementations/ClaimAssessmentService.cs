using FluentValidation;
using Microsoft.Extensions.Logging;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Application.Claims.Services.Implementations;

public class ClaimAssessmentService : IClaimAssessmentService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimPaymentService _paymentService;
    private readonly IValidator<AssessClaimRequest> _assessValidator;
    private readonly ILogger<ClaimAssessmentService> _logger;

    public ClaimAssessmentService(
        IClaimRepository claimRepository,
        IClaimPaymentService paymentService,
        IValidator<AssessClaimRequest> assessValidator,
        ILogger<ClaimAssessmentService> logger)
    {
        _claimRepository = claimRepository;
        _paymentService = paymentService;
        _assessValidator = assessValidator;
        _logger = logger;
    }

    public async Task<Claim> AssessAsync(Guid claimId, AssessClaimRequest request, CancellationToken cancellationToken)
    {
        await _assessValidator.ValidateAndThrowAsync(request, cancellationToken);

        var claim = await _claimRepository.GetByIdAsync(claimId, cancellationToken)
            ?? throw new ClaimNotFoundException(claimId);

        if (request.Decision == AssessmentDecision.Approve)
        {
            claim.Approve(request.ApprovedAmount!.Value, request.AssessedBy, request.Notes);
        }
        else
        {
            claim.Reject(request.AssessedBy, request.Notes);
        }

        await _claimRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Claim {ClaimNumber} {Decision} by {AssessedBy}",
            claim.ClaimNumber,
            request.Decision,
            request.AssessedBy);

        if (claim.Status == ClaimStatus.Approved)
        {
            return await _paymentService.RequestPaymentAsync(claim.Id, cancellationToken);
        }

        return claim;
    }
}
