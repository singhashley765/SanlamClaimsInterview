using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Domain.Entities;

public class ClaimStatusHistory
{
    internal ClaimStatusHistory(Guid claimId, ClaimStatus? fromStatus, ClaimStatus toStatus, string changedBy, string? reason, DateTime changedAt)
    {
        Id = Guid.NewGuid();
        ClaimId = claimId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedBy = changedBy;
        Reason = reason;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public Guid ClaimId { get; private set; }

    public ClaimStatus? FromStatus { get; private set; }

    public ClaimStatus ToStatus { get; private set; }

    public string ChangedBy { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    public DateTime ChangedAt { get; private set; }
}
