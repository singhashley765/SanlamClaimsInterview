using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SanlamClaims.Application.Claims.Services.Implementations;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Application.Common.Interfaces;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;
using SanlamClaims.Domain.Interfaces;

namespace SanlamClaims.Tests.Unit.Claims.Services;

[TestClass]
public class ClaimPaymentServiceTests
{
    private Mock<IClaimRepository> _claimRepository = null!;
    private Mock<IPaymentClient> _paymentClient = null!;
    private Mock<IPaymentRequestPublisher> _paymentRequestPublisher = null!;
    private Claim _claim = null!;
    private ClaimPaymentService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _claim = Claim.Register(
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
        _claim.Approve(40_000m, "analyst@sanlam.co.za", "Approved.");

        _claimRepository = new Mock<IClaimRepository>();
        _claimRepository.Setup(r => r.GetByIdAsync(_claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_claim);

        _paymentClient = new Mock<IPaymentClient>();
        _paymentRequestPublisher = new Mock<IPaymentRequestPublisher>();

        var correlationIdAccessor = new Mock<ICorrelationIdAccessor>();

        _sut = new ClaimPaymentService(_claimRepository.Object, _paymentClient.Object, _paymentRequestPublisher.Object, correlationIdAccessor.Object, NullLogger<ClaimPaymentService>.Instance);
    }

    [TestMethod]
    public async Task RequestPaymentAsync_ApprovedClaim_PublishesRequestAndLeavesClaimUnchanged()
    {
        var result = await _sut.RequestPaymentAsync(_claim.Id, CancellationToken.None);

        result.Status.Should().Be(ClaimStatus.Approved);
        _paymentRequestPublisher.Verify(
            p => p.PublishAsync(It.Is<PaymentRequestedMessage>(m => m.ClaimId == _claim.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RequestPaymentAsync_ClaimNotYetApproved_ThrowsInvalidTransitionWithoutPublishing()
    {
        var underAssessmentClaim = Claim.Register(
            "CLM-2026-000002",
            ClaimType.FuneralCover,
            "Thandiwe",
            "Mokoena",
            "9202124800080",
            "0821234568",
            "thandiwe@example.com",
            null,
            "Thandiwe Mokoena",
            "POL-100002",
            750_000m,
            DateTime.UtcNow.AddHours(4),
            "web-form-intake");

        _claimRepository.Setup(r => r.GetByIdAsync(underAssessmentClaim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(underAssessmentClaim);

        var act = async () => await _sut.RequestPaymentAsync(underAssessmentClaim.Id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidClaimStateTransitionException>();
        _paymentRequestPublisher.Verify(p => p.PublishAsync(It.IsAny<PaymentRequestedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_Success_CompletesPayment()
    {
        _paymentClient.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(true, "PMT-1234", null));

        var result = await _sut.ProcessPaymentAsync(_claim.Id, CancellationToken.None);

        result.Status.Should().Be(ClaimStatus.PaymentCompleted);
        result.PaymentReference.Should().Be(_claim.Id.ToString());
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_Declined_FailsPaymentWithReason()
    {
        _paymentClient.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(false, null, "Insufficient funds."));

        var result = await _sut.ProcessPaymentAsync(_claim.Id, CancellationToken.None);

        result.Status.Should().Be(ClaimStatus.PaymentFailed);
        result.PaymentFailureReason.Should().Be("Insufficient funds.");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_ExternalSystemUnavailable_GracefullyFailsPaymentInsteadOfThrowing()
    {
        _paymentClient.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalSystemException("Payment", "Payment system call failed.", new HttpRequestException()));

        var result = await _sut.ProcessPaymentAsync(_claim.Id, CancellationToken.None);

        result.Status.Should().Be(ClaimStatus.PaymentFailed);
        result.PaymentFailureReason.Should().Contain("unavailable");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_ClaimNotFound_ThrowsClaimNotFoundException()
    {
        _claimRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Claim?)null);

        var act = async () => await _sut.ProcessPaymentAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ClaimNotFoundException>();
    }
}
