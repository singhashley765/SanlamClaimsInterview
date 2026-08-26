using FluentValidation;
using Microsoft.Extensions.Logging;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Interfaces;
using SanlamClaims.Domain.Policies.Interfaces;

namespace SanlamClaims.Application.Claims.Services.Implementations;

public class ClaimSubmissionService : IClaimSubmissionService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimNumberGenerator _claimNumberGenerator;
    private readonly IClientRegistryClient _clientRegistryClient;
    private readonly IPolicyManagementClient _policyManagementClient;
    private readonly IClaimTypePolicyProvider _claimTypePolicyProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<SubmitClaimRequest> _validator;
    private readonly ILogger<ClaimSubmissionService> _logger;

    public ClaimSubmissionService(
        IClaimRepository claimRepository,
        IClaimNumberGenerator claimNumberGenerator,
        IClientRegistryClient clientRegistryClient,
        IPolicyManagementClient policyManagementClient,
        IClaimTypePolicyProvider claimTypePolicyProvider,
        IDateTimeProvider dateTimeProvider,
        IValidator<SubmitClaimRequest> validator,
        ILogger<ClaimSubmissionService> logger)
    {
        _claimRepository = claimRepository;
        _claimNumberGenerator = claimNumberGenerator;
        _clientRegistryClient = clientRegistryClient;
        _policyManagementClient = policyManagementClient;
        _claimTypePolicyProvider = claimTypePolicyProvider;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Claim> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        _logger.LogInformation(
            "Submitting {ClaimType} claim for id number {IdNumber}, policy {PolicyNumber}",
            request.ClaimType,
            request.IdNumber,
            request.PolicyNumber);

        var clientTask = _clientRegistryClient.GetClientByIdNumberAsync(request.IdNumber, cancellationToken);
        var policyTask = _policyManagementClient.GetPolicyAsync(request.PolicyNumber, cancellationToken);
        await Task.WhenAll(clientTask, policyTask);

        var client = clientTask.Result ?? throw new ClientNotFoundException(request.IdNumber);
        var policy = policyTask.Result ?? throw new PolicyNotFoundException(request.PolicyNumber);

        if (!string.Equals(policy.ClientId, client.ClientId, StringComparison.OrdinalIgnoreCase))
        {
            throw new PolicyClientMismatchException(request.PolicyNumber, client.ClientId);
        }

        var claimNumber = await _claimNumberGenerator.NextAsync(cancellationToken);
        var typePolicy = _claimTypePolicyProvider.GetPolicy(request.ClaimType);

        var duplicateOfClaimId = await _claimRepository.FindAssessedDuplicateAsync(
            request.IdNumber, request.PolicyNumber, request.ClaimType, cancellationToken);

        // SLA is measured from the moment the claim is submitted
        var resolutionDueAt = _dateTimeProvider.UtcNow.AddMinutes(typePolicy.SlaResolutionMinutes);

        var claim = Claim.Register(
            claimNumber,
            request.ClaimType,
            request.FirstNames,
            request.Surname,
            request.IdNumber,
            request.CellphoneNumber,
            request.EmailAddress,
            request.Message,
            client.FullName,
            policy.PolicyNumber,
            policy.CoverageAmount,
            resolutionDueAt,
            request.Application.ToString(),
            duplicateOfClaimId);

        await _claimRepository.AddAsync(claim, cancellationToken);

        _logger.LogInformation(
            "Claim {ClaimNumber} registered ({ClaimId}) and queued for assessment, SLA due at {ResolutionDueAt:u}",
            claim.ClaimNumber,
            claim.Id,
            claim.ResolutionDueAt);

        return claim;
    }
}
