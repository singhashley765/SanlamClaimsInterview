namespace SanlamClaims.Application.Common.Exceptions;

public class PolicyNotFoundException : Exception
{
    public PolicyNotFoundException(string policyNumber)
        : base($"Policy '{policyNumber}' was not found in the Policy Management system.")
    {
        PolicyNumber = policyNumber;
    }

    public string PolicyNumber { get; }
}
