using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
namespace CorporateStarter.Client.Core.MasterData.Cities;

public sealed class CitiesClient(ClientApiClient api)
    : MasterDataClient<CityListItemDto, CityDetailsDto, CreateCityRequest, UpdateCityRequest>(
        api, "/api/Cities", ClientJsonContext.Default.CityPage, ClientJsonContext.Default.Cities,
        ClientJsonContext.Default.CityDetailsDto, ClientJsonContext.Default.CreateCityRequest,
        ClientJsonContext.Default.UpdateCityRequest, x => x.Id, x => x.Name is not null,
        x => x.Id, x => !string.IsNullOrWhiteSpace(x.Name))
{
    public Task<TableCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync("/api/Cities/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) => CapabilitiesAsync(ct);
    public async Task<LookupOption[]> SearchCountriesAsync(string text, CancellationToken ct = default)
    {
        var result = await Api.PostJsonAsync("/api/Cities/country-options", new LookupRequest { Text = text },
            ClientJsonContext.Default.LookupRequest, ClientJsonContext.Default.LookupOptions, ct);
        if (result.Length > 20 || result.Any(x => x is null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.Label)) ||
            result.Select(x => x.Id).Distinct().Count() != result.Length)
            throw new InvalidDataException("The API returned invalid lookup options.");
        return result;
    }
}
