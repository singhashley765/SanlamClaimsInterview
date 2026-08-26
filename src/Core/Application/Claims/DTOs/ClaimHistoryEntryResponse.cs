using SanlamClaims.Domain.Enums;

namespace SanlamClaims.Application.Claims.DTOs;

public sealed record ClaimHistoryEntryResponse(
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    string ChangedBy,
    string? Reason,
    DateTime ChangedAt);
