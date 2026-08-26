namespace SanlamClaims.Application.Common.Exceptions;

public class ExternalSystemException : Exception
{
    public ExternalSystemException(string systemName, string message, Exception innerException)
        : base(message, innerException)
    {
        SystemName = systemName;
    }

    public string SystemName { get; }
}
