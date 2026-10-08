using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Common;
namespace CorporateStarter.Client.Core.Lookups;

internal static class LookupQuery
{
    public static async Task<LookupOption[]> SearchAsync(ClientApiClient api, string route, string text, CancellationToken ct)
    {
        var result = await api.PostJsonAsync(route, new LookupRequest { Text = text },
            ClientJsonContext.Default.LookupRequest, ClientJsonContext.Default.LookupOptions, ct);
        if (result.Length > 20 || result.Any(x => x is null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.Label)) ||
            result.Select(x => x.Id).Distinct().Count() != result.Length)
            throw new InvalidDataException("The API returned invalid lookup options.");
        return result;
    }
}
