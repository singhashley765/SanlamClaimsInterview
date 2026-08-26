using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Exceptions;

namespace SanlamClaims.Domain.Entities;

public class Claim
{
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> AllowedTransitions =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Registered] = [ClaimStatus.UnderAssessment],
            [ClaimStatus.UnderAssessment] = [ClaimStatus.Approved, ClaimStatus.Rejected],
            [ClaimStatus.Approved] = [ClaimStatus.PaymentInitiated],
            [ClaimStatus.Rejected] = [],
            [ClaimStatus.PaymentInitiated] = [ClaimStatus.PaymentCompleted, ClaimStatus.PaymentFailed],
            [ClaimStatus.PaymentCompleted] = [],
            [ClaimStatus.PaymentFailed] = [ClaimStatus.PaymentInitiated],
        };

    private readonly List<ClaimStatusHistory> _statusHistory = [];

    private Claim()
    {
    }

    public Guid Id { get; private set; }

    public string ClaimNumber { get; private set; } = string.Empty;

    public ClaimType ClaimType { get; private set; }

    public ClaimStatus Status { get; private set; }

    public string FirstNames { get; private set; } = string.Empty;

    public string Surname { get; private set; } = string.Empty;

    public string IdNumber { get; private set; } = string.Empty;

    public string CellphoneNumber { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public string? Message { get; private set; }

    public string ClientFullName { get; private set; } = string.Empty;

    public string PolicyNumber { get; private set; } = string.Empty;

    public decimal CoverageAmount { get; private set; }

    public DateTime ResolutionDueAt { get; private set; }

    public decimal? ApprovedAmount { get; private set; }

    public string? AssessmentNotes { get; private set; }

    public string? AssessedBy { get; private set; }

    public DateTime? AssessedAt { get; private set; }

    public string? PaymentReference { get; private set; }

    public DateTime? PaymentInitiatedAt { get; private set; }

    public DateTime? PaymentCompletedAt { get; private set; }

    public string? PaymentFailureReason { get; private set; }

    public bool IsPossibleDuplicate { get; private set; }

    public Guid? DuplicateOfClaimId { get; private set; }

    public byte[]? RowVersion { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<ClaimStatusHistory> StatusHistory => _statusHistory;

    public bool IsSlaBreached => AssessedAt.HasValue
        ? AssessedAt.Value > ResolutionDueAt
        : DateTime.UtcNow > ResolutionDueAt;

    public static Claim Register(
        string claimNumber,
        ClaimType claimType,
        string firstNames,
        string surname,
        string idNumber,
        string cellphoneNumber,
        string emailAddress,
        string? message,
        string clientFullName,
        string policyNumber,
        decimal coverageAmount,
        DateTime resolutionDueAt,
        string registeredBy,
        Guid? duplicateOfClaimId = null)
    {
        var now = DateTime.UtcNow;

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = claimNumber,
            ClaimType = claimType,
            Status = ClaimStatus.Registered,
            FirstNames = firstNames,
            Surname = surname,
            IdNumber = idNumber,
            CellphoneNumber = cellphoneNumber,
            EmailAddress = emailAddress,
            Message = message,
            ClientFullName = clientFullName,
            PolicyNumber = policyNumber,
            CoverageAmount = coverageAmount,
            ResolutionDueAt = resolutionDueAt,
            IsPossibleDuplicate = duplicateOfClaimId.HasValue,
            DuplicateOfClaimId = duplicateOfClaimId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var registrationReason = duplicateOfClaimId.HasValue
            ? $"Claim registered after client and policy verification. Possible duplicate of claim {duplicateOfClaimId}."
            : "Claim registered after client and policy verification.";

        claim._statusHistory.Add(new ClaimStatusHistory(
            claim.Id,
            fromStatus: null,
            ClaimStatus.Registered,
            registeredBy,
            registrationReason,
            now));

        claim.TransitionTo(ClaimStatus.UnderAssessment, registeredBy, "Queued for Claims Analyst review.");

        return claim;
    }

    public void Approve(decimal approvedAmount, string assessedBy, string? notes)
    {
        TransitionTo(ClaimStatus.Approved, assessedBy, notes ?? "Claim approved.");
        ApprovedAmount = approvedAmount;
        AssessedBy = assessedBy;
        AssessedAt = UpdatedAt;
        AssessmentNotes = notes;
    }

    public void Reject(string assessedBy, string reason)
    {
        TransitionTo(ClaimStatus.Rejected, assessedBy, reason);
        AssessedBy = assessedBy;
        AssessedAt = UpdatedAt;
        AssessmentNotes = reason;
    }

    public void InitiatePayment(string paymentReference, string changedBy)
    {
        TransitionTo(ClaimStatus.PaymentInitiated, changedBy, $"Payment initiated (reference {paymentReference}).");
        PaymentReference = paymentReference;
        PaymentInitiatedAt = UpdatedAt;
        PaymentFailureReason = null;
    }

    public void CompletePayment(string changedBy)
    {
        TransitionTo(ClaimStatus.PaymentCompleted, changedBy, "Payment completed.");
        PaymentCompletedAt = UpdatedAt;
    }

    public void FailPayment(string failureReason, string changedBy)
    {
        TransitionTo(ClaimStatus.PaymentFailed, changedBy, failureReason);
        PaymentFailureReason = failureReason;
    }

    private void TransitionTo(ClaimStatus newStatus, string changedBy, string? reason)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidClaimStateTransitionException(Status, newStatus);
        }

        var from = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(new ClaimStatusHistory(Id, from, newStatus, changedBy, reason, UpdatedAt));
    }
}
