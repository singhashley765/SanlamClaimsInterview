using SanlamClaims.Domain.Enums;
using SanlamClaims.Domain.Policies.Interfaces;

namespace SanlamClaims.Domain.Policies.Implementations;

public class ClaimTypePolicyProvider : IClaimTypePolicyProvider
{
    private static readonly IReadOnlyDictionary<ClaimType, ClaimTypePolicy> Policies =
        new Dictionary<ClaimType, ClaimTypePolicy>
        {
            // Death-related payouts get a fast SLA.
            [ClaimType.FuneralCover] = new ClaimTypePolicy(ClaimType.FuneralCover, SlaResolutionMinutes: 4 * 60),
            [ClaimType.FuneralBenefitFromLifeCover] = new ClaimTypePolicy(ClaimType.FuneralBenefitFromLifeCover, SlaResolutionMinutes: 4 * 60),
            [ClaimType.DigitalFuneralCover] = new ClaimTypePolicy(ClaimType.DigitalFuneralCover, SlaResolutionMinutes: 4 * 60),
            [ClaimType.LifeCoverSavingsPolicy] = new ClaimTypePolicy(ClaimType.LifeCoverSavingsPolicy, SlaResolutionMinutes: 4 * 60),

            // Urgent but not death claims.
            [ClaimType.HospitalAdmissionOneMedicalPlan] = new ClaimTypePolicy(ClaimType.HospitalAdmissionOneMedicalPlan, SlaResolutionMinutes: 24 * 60),
            [ClaimType.Covid19Claim] = new ClaimTypePolicy(ClaimType.Covid19Claim, SlaResolutionMinutes: 24 * 60),

            // Standard turnaround.
            [ClaimType.SicknessBenefit] = new ClaimTypePolicy(ClaimType.SicknessBenefit, SlaResolutionMinutes: 2 * 24 * 60),
            [ClaimType.MedicalGapCover] = new ClaimTypePolicy(ClaimType.MedicalGapCover, SlaResolutionMinutes: 2 * 24 * 60),
            [ClaimType.SevereIllnessDreadDiseaseOrChildIllness] = new ClaimTypePolicy(ClaimType.SevereIllnessDreadDiseaseOrChildIllness, SlaResolutionMinutes: 3 * 24 * 60),
            [ClaimType.DisabilityOrAccidentBenefits] = new ClaimTypePolicy(ClaimType.DisabilityOrAccidentBenefits, SlaResolutionMinutes: 3 * 24 * 60),
            [ClaimType.IncomeProtection] = new ClaimTypePolicy(ClaimType.IncomeProtection, SlaResolutionMinutes: 5 * 24 * 60),

            // The claimant didn't know which product applies
            [ClaimType.NotSure] = new ClaimTypePolicy(ClaimType.NotSure, SlaResolutionMinutes: 3 * 24 * 60),
        };

    public ClaimTypePolicy GetPolicy(ClaimType claimType)
    {
        if (!Policies.TryGetValue(claimType, out var policy))
        {
            throw new ArgumentOutOfRangeException(nameof(claimType), claimType, "No policy configured for this claim type.");
        }

        return policy;
    }
}
