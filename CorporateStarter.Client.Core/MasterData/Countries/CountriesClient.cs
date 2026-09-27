using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Client.Core.MasterData.Countries;

public sealed class CountriesClient(ClientApiClient api)
    : MasterDataClient<CountryListItemDto, CountryDetailsDto, CreateCountryRequest, UpdateCountryRequest>(
        api, "/api/Countries", ClientJsonContext.Default.CountryPage, ClientJsonContext.Default.Countries,
        ClientJsonContext.Default.CountryDetailsDto, ClientJsonContext.Default.CreateCountryRequest,
        ClientJsonContext.Default.UpdateCountryRequest, x => x.Id, x => x.Code is not null && x.Name is not null,
        x => x.Id, x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name))
{
    public Task<CountryCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync("/api/Countries/capabilities", ClientJsonContext.Default.CountryCapabilities, ct);
    public override async Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default)
    {
        var c = await CapabilitiesAsync(ct).ConfigureAwait(false);
        return new(c.CanCreate, c.CanUpdate, c.CanDelete, c.ViewInactive, c.CanRestore);
    }
}
