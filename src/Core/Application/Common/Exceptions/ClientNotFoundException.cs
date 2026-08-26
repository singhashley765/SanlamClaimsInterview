namespace SanlamClaims.Application.Common.Exceptions;

public class ClientNotFoundException : Exception
{
    public ClientNotFoundException(string clientId)
        : base($"Client '{clientId}' was not found in the Client Registry.")
    {
        ClientId = clientId;
    }

    public string ClientId { get; }
}
