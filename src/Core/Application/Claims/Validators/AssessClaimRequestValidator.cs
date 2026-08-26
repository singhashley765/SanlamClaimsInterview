using FluentValidation;
using SanlamClaims.Application.Claims.DTOs;

namespace SanlamClaims.Application.Claims.Validators;

public class AssessClaimRequestValidator : AbstractValidator<AssessClaimRequest>
{
    public AssessClaimRequestValidator()
    {
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.AssessedBy).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.ApprovedAmount)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.Decision == AssessmentDecision.Approve)
            .WithMessage("Approved amount is required and must be greater than zero when approving a claim.");
    }
}
