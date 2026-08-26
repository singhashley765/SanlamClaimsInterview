using System.Text.Json;

namespace SanlamClaims.Infrastructure.ExternalClients;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
