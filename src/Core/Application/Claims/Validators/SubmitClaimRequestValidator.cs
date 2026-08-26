using FluentValidation;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Domain.Common;

namespace SanlamClaims.Application.Claims.Validators;

public class SubmitClaimRequestValidator : AbstractValidator<SubmitClaimRequest>
{
    private const string CellphonePattern = @"^(\+27|0)[6-8][0-9]{8}$";

    public SubmitClaimRequestValidator()
    {
        RuleFor(x => x.Application).IsInEnum();
        RuleFor(x => x.ClaimType).IsInEnum();
        RuleFor(x => x.FirstNames).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(128);

        RuleFor(x => x.IdNumber)
            .NotEmpty()
            .Must(SouthAfricanIdNumber.IsValid)
            .WithMessage("'{PropertyName}' is not a valid South African ID number.");

        RuleFor(x => x.PolicyNumber).NotEmpty().MaximumLength(64);

        RuleFor(x => x.CellphoneNumber)
            .NotEmpty()
            .Matches(CellphonePattern)
            .WithMessage("'{PropertyName}' must be a valid South African cellphone number.");

        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();

        RuleFor(x => x.Message).MaximumLength(2000);
    }
}
