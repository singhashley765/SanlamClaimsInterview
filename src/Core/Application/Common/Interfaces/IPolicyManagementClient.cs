namespace SanlamClaims.Application.Common.Interfaces;

public interface IPolicyManagementClient
{
    Task<PolicyDetails?> GetPolicyAsync(string policyNumber, CancellationToken cancellationToken);
}
