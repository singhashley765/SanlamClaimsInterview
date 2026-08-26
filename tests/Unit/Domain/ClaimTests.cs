using FluentAssertions;
using SanlamClaims.Domain.Entities;
using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;

namespace SanlamClaims.Tests.Unit.Domain;

[TestClass]
public class ClaimTests
{
    [TestMethod]
    public void Register_QueuesClaimForAssessment()
    {
        var claim = CreateRegisteredClaim();

        claim.Status.Should().Be(ClaimStatus.UnderAssessment);
        claim.StatusHistory.Should().HaveCount(2);
        claim.StatusHistory.First().ToStatus.Should().Be(ClaimStatus.Registered);
        claim.StatusHistory.Last().ToStatus.Should().Be(ClaimStatus.UnderAssessment);
    }

    [TestMethod]
    public void Register_WithoutDuplicateOfClaimId_LeavesDuplicateFlagUnset()
    {
        var claim = CreateRegisteredClaim();

        claim.IsPossibleDuplicate.Should().BeFalse();
        claim.DuplicateOfClaimId.Should().BeNull();
    }

    [TestMethod]
    public void Register_WithDuplicateOfClaimId_FlagsClaimAsPossibleDuplicate()
    {
        var earlierClaimId = Guid.NewGuid();

        var claim = Claim.Register(
            "CLM-2026-000002",
            ClaimType.FuneralCover,
            "John",
            "Ndlovu",
            "8501015800088",
            "0821234567",
            "john.ndlovu@example.com",
            "Resubmitting my claim.",
            "John Ndlovu",
            "POL-100001",
            500_000m,
            DateTime.UtcNow.AddHours(4),
            "web-form-intake",
            earlierClaimId);

        claim.IsPossibleDuplicate.Should().BeTrue();
        claim.DuplicateOfClaimId.Should().Be(earlierClaimId);
        claim.StatusHistory.First().Reason.Should().Contain(earlierClaimId.ToString());
    }

    [TestMethod]
    public void InitiatePayment_BeforeApproval_ThrowsInvalidTransition()
    {
        var claim = CreateRegisteredClaim();

        var act = () => claim.InitiatePayment("idem-key-1", "system-payment-processor");

        act.Should().Throw<InvalidClaimStateTransitionException>()
            .Which.From.Should().Be(ClaimStatus.UnderAssessment);
    }

    [TestMethod]
    public void FullHappyPath_TransitionsThroughAllStatuses()
    {
        var claim = CreateRegisteredClaim();

        claim.Approve(40_000m, "analyst@sanlam.co.za", "Approved after review.");
        claim.Status.Should().Be(ClaimStatus.Approved);
        claim.ApprovedAmount.Should().Be(40_000m);
        claim.AssessedAt.Should().NotBeNull();

        claim.InitiatePayment("idem-key-1", "system-payment-processor");
        claim.Status.Should().Be(ClaimStatus.PaymentInitiated);
        claim.PaymentReference.Should().Be("idem-key-1");

        claim.CompletePayment("system-payment-processor");
        claim.Status.Should().Be(ClaimStatus.PaymentCompleted);
        claim.PaymentCompletedAt.Should().NotBeNull();

        claim.StatusHistory.Should().HaveCount(5);
    }

    [TestMethod]
    public void FailPayment_ThenRetry_TransitionsBackToPaymentInitiatedAndClearsFailureReason()
    {
        var claim = CreateRegisteredClaim();
        claim.Approve(40_000m, "analyst@sanlam.co.za", "Approved.");
        claim.InitiatePayment("idem-key-1", "system-payment-processor");

        claim.FailPayment("Payment system unavailable.", "system-payment-processor");
        claim.Status.Should().Be(ClaimStatus.PaymentFailed);

        claim.InitiatePayment("idem-key-1", "system-payment-processor");

        claim.Status.Should().Be(ClaimStatus.PaymentInitiated);
        claim.PaymentFailureReason.Should().BeNull();
    }

    [TestMethod]
    public void Reject_SetsAssessmentFieldsAndTerminalStatus()
    {
        var claim = CreateRegisteredClaim();

        claim.Reject("analyst@sanlam.co.za", "Policy lapsed.");

        claim.Status.Should().Be(ClaimStatus.Rejected);
        claim.AssessmentNotes.Should().Be("Policy lapsed.");
        claim.ApprovedAmount.Should().BeNull();
    }

    [TestMethod]
    public void IsSlaBreached_WhenAssessedAfterDueDate_IsTrue()
    {
        var claim = CreateRegisteredClaim(resolutionDueAt: DateTime.UtcNow.AddMinutes(-10));

        claim.Reject("analyst@sanlam.co.za", "Too slow.");

        claim.IsSlaBreached.Should().BeTrue();
    }

    [TestMethod]
    public void IsSlaBreached_WhenStillOpenAndWithinSla_IsFalse()
    {
        var claim = CreateRegisteredClaim(resolutionDueAt: DateTime.UtcNow.AddHours(4));

        claim.IsSlaBreached.Should().BeFalse();
    }

    private static Claim CreateRegisteredClaim(DateTime? resolutionDueAt = null) => Claim.Register(
        "CLM-2026-000001",
        ClaimType.FuneralCover,
        "John",
        "Ndlovu",
        "8501015800088",
        "0821234567",
        "john.ndlovu@example.com",
        "My father passed away last week.",
        "John Ndlovu",
        "POL-100001",
        500_000m,
        resolutionDueAt ?? DateTime.UtcNow.AddHours(4),
        "web-form-intake");
}
