namespace SanlamClaims.Application.Common.Exceptions;

public class PolicyClientMismatchException : Exception
{
    public PolicyClientMismatchException(string policyNumber, string clientId)
        : base($"Policy '{policyNumber}' does not belong to client '{clientId}'.")
    {
        PolicyNumber = policyNumber;
        ClientId = clientId;
    }

    public string PolicyNumber { get; }

    public string ClientId { get; }
}
