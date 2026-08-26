using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Application.Claims.Services.Implementations;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Tests.Unit.Claims.Services;

[TestClass]
public class ClaimAssessmentServiceTests
{
    private Mock<IClaimRepository> _claimRepository = null!;
    private Mock<IClaimPaymentService> _paymentService = null!;
    private Mock<IValidator<AssessClaimRequest>> _assessValidator = null!;
    private Claim _claim = null!;
    private ClaimAssessmentService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _claim = CreateUnderAssessmentClaim();

        _claimRepository = new Mock<IClaimRepository>();
        _claimRepository.Setup(r => r.GetByIdAsync(_claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_claim);

        _paymentService = new Mock<IClaimPaymentService>();
        _paymentService.Setup(p => p.RequestPaymentAsync(_claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_claim);

        _assessValidator = new Mock<IValidator<AssessClaimRequest>>();
        _assessValidator.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new ClaimAssessmentService(_claimRepository.Object, _paymentService.Object, _assessValidator.Object, NullLogger<ClaimAssessmentService>.Instance);
    }

    [TestMethod]
    public async Task AssessAsync_Approve_ApprovesClaimAndInitiatesPayment()
    {
        var request = new AssessClaimRequest(AssessmentDecision.Approve, 40_000m, "Verified with documentation.", "analyst@sanlam.co.za");

        await _sut.AssessAsync(_claim.Id, request, CancellationToken.None);

        _claim.Status.Should().Be(ClaimStatus.Approved);
        _claim.ApprovedAmount.Should().Be(40_000m);
        _paymentService.Verify(p => p.RequestPaymentAsync(_claim.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AssessAsync_Reject_RejectsClaimWithoutInitiatingPayment()
    {
        var request = new AssessClaimRequest(AssessmentDecision.Reject, null, "Policy was lapsed at time of claim.", "analyst@sanlam.co.za");

        await _sut.AssessAsync(_claim.Id, request, CancellationToken.None);

        _claim.Status.Should().Be(ClaimStatus.Rejected);
        _claim.AssessmentNotes.Should().Be("Policy was lapsed at time of claim.");
        _paymentService.Verify(p => p.RequestPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Claim CreateUnderAssessmentClaim() => Claim.Register(
        "CLM-2026-000001",
        ClaimType.FuneralCover,
        "John",
        "Ndlovu",
        "8501015800088",
        "0821234567",
        "john.ndlovu@example.com",
        "My father passed away.",
        "John Ndlovu",
        "POL-100001",
        500_000m,
        DateTime.UtcNow.AddHours(4),
        "web-form-intake");
}
