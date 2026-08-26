using SanlamClaims.Application.Common.Interfaces;

namespace SanlamClaims.Application.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
