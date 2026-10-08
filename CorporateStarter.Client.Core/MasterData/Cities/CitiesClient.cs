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
    public Task<LookupOption[]> SearchCountriesAsync(string text, CancellationToken ct = default) =>
        CorporateStarter.Client.Core.Lookups.LookupQuery.SearchAsync(Api, "/api/Cities/country-options", text, ct);
}
