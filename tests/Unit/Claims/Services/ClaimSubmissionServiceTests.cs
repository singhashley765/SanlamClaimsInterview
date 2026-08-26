using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Application.Claims.Services.Implementations;
using SanlamClaims.Application.Common;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Interfaces;
using SanlamClaims.Domain.Policies.Implementations;

namespace SanlamClaims.Tests.Unit.Claims.Services;

[TestClass]
public class ClaimSubmissionServiceTests
{
    private Mock<IClaimRepository> _claimRepository = null!;
    private Mock<IClaimNumberGenerator> _claimNumberGenerator = null!;
    private Mock<IClientRegistryClient> _clientRegistryClient = null!;
    private Mock<IPolicyManagementClient> _policyManagementClient = null!;
    private ClaimSubmissionService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _claimRepository = new Mock<IClaimRepository>();
        _claimNumberGenerator = new Mock<IClaimNumberGenerator>();
        _clientRegistryClient = new Mock<IClientRegistryClient>();
        _policyManagementClient = new Mock<IPolicyManagementClient>();

        var validator = new Mock<IValidator<SubmitClaimRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _claimNumberGenerator.Setup(g => g.NextAsync(It.IsAny<CancellationToken>())).ReturnsAsync("CLM-2026-000001");

        _sut = new ClaimSubmissionService(
            _claimRepository.Object,
            _claimNumberGenerator.Object,
            _clientRegistryClient.Object,
            _policyManagementClient.Object,
            new ClaimTypePolicyProvider(),
            new SystemDateTimeProvider(),
            validator.Object,
            NullLogger<ClaimSubmissionService>.Instance);
    }

    [TestMethod]
    public async Task SubmitAsync_HappyPath_RegistersClaimQueuedForAssessment()
    {
        var request = CreateRequest();

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDetails("C-1001", "John Ndlovu"));
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDetails(request.PolicyNumber, "C-1001", 50_000m));

        Claim? addedClaim = null;
        _claimRepository.Setup(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Callback<Claim, CancellationToken>((claim, _) => addedClaim = claim)
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(request, CancellationToken.None);

        result.Should().BeSameAs(addedClaim);
        result.ClaimNumber.Should().Be("CLM-2026-000001");
        result.Status.Should().Be(ClaimStatus.UnderAssessment);
        result.ClientFullName.Should().Be("John Ndlovu");
        result.PolicyNumber.Should().Be(request.PolicyNumber);
    }

    [TestMethod]
    public async Task SubmitAsync_MatchingAssessedClaimExists_FlagsNewClaimAsPossibleDuplicate()
    {
        var request = CreateRequest();
        var earlierClaimId = Guid.NewGuid();

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDetails("C-1001", "John Ndlovu"));
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDetails(request.PolicyNumber, "C-1001", 50_000m));
        _claimRepository.Setup(r => r.FindAssessedDuplicateAsync(request.IdNumber, request.PolicyNumber, request.ClaimType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(earlierClaimId);

        var result = await _sut.SubmitAsync(request, CancellationToken.None);

        result.IsPossibleDuplicate.Should().BeTrue();
        result.DuplicateOfClaimId.Should().Be(earlierClaimId);
    }

    [TestMethod]
    public async Task SubmitAsync_NoMatchingAssessedClaim_LeavesDuplicateFlagUnset()
    {
        var request = CreateRequest();

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDetails("C-1001", "John Ndlovu"));
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDetails(request.PolicyNumber, "C-1001", 50_000m));
        _claimRepository.Setup(r => r.FindAssessedDuplicateAsync(request.IdNumber, request.PolicyNumber, request.ClaimType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var result = await _sut.SubmitAsync(request, CancellationToken.None);

        result.IsPossibleDuplicate.Should().BeFalse();
        result.DuplicateOfClaimId.Should().BeNull();
    }

    [TestMethod]
    public async Task SubmitAsync_ClientNotFound_ThrowsClientNotFoundException()
    {
        var request = CreateRequest();

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>())).ReturnsAsync((ClientDetails?)null);
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDetails(request.PolicyNumber, "C-1001", 50_000m));

        var act = async () => await _sut.SubmitAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [TestMethod]
    public async Task SubmitAsync_PolicyNotFound_ThrowsPolicyNotFoundException()
    {
        var request = CreateRequest(policyNumber: "POL-999999");

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDetails("C-1001", "John Ndlovu"));
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>())).ReturnsAsync((PolicyDetails?)null);

        var act = async () => await _sut.SubmitAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<PolicyNotFoundException>();
    }

    [TestMethod]
    public async Task SubmitAsync_PolicyBelongsToDifferentClient_ThrowsPolicyClientMismatchException()
    {
        var request = CreateRequest();

        _clientRegistryClient.Setup(c => c.GetClientByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDetails("C-1001", "John Ndlovu"));
        _policyManagementClient.Setup(c => c.GetPolicyAsync(request.PolicyNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDetails(request.PolicyNumber, "C-2002", 50_000m));

        var act = async () => await _sut.SubmitAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<PolicyClientMismatchException>();
    }

    private static SubmitClaimRequest CreateRequest(string idNumber = "8501015800088", string policyNumber = "POL-100001") => new SubmitClaimRequest(
        SubmissionChannel.WebForm,
        ClaimType.FuneralCover,
        "John",
        "Ndlovu",
        idNumber,
        policyNumber,
        "0821234567",
        "john.ndlovu@example.com",
        "My father passed away.");
}
