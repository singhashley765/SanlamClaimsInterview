namespace SanlamClaims.Application.Common.Interfaces;

public interface IClientRegistryClient
{
    Task<ClientDetails?> GetClientByIdNumberAsync(string idNumber, CancellationToken cancellationToken);
}
