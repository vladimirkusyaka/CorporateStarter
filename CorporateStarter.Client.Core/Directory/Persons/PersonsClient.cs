using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
namespace CorporateStarter.Client.Core.Directory.Persons;

public sealed class PersonsClient(ClientApiClient api)
    : MasterDataClient<PersonListItemDto, PersonDetailsDto, CreatePersonRequest, UpdatePersonRequest>(
        api, "/api/Persons", ClientJsonContext.Default.PersonPage, ClientJsonContext.Default.Persons,
        ClientJsonContext.Default.PersonDetailsDto, ClientJsonContext.Default.CreatePersonRequest,
        ClientJsonContext.Default.UpdatePersonRequest, x => x.Id, x => x.FirstName is not null,
        x => x.Id, x => !string.IsNullOrWhiteSpace(x.FirstName))
{
    public Task<TableCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync("/api/Persons/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) => CapabilitiesAsync(ct);
    public Task<LookupOption[]> SearchCompaniesAsync(string text, CancellationToken ct = default) =>
        CorporateStarter.Client.Core.Lookups.LookupQuery.SearchAsync(Api, "/api/Persons/company-options", text, ct);
    public Task<LookupOption[]> SearchPositionsAsync(string text, CancellationToken ct = default) =>
        CorporateStarter.Client.Core.Lookups.LookupQuery.SearchAsync(Api, "/api/Persons/position-options", text, ct);
}
