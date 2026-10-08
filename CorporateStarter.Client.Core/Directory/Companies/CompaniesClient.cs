using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
namespace CorporateStarter.Client.Core.Directory.Companies;

public sealed class CompaniesClient(ClientApiClient api)
    : MasterDataClient<CompanyListItemDto, CompanyDetailsDto, CreateCompanyRequest, UpdateCompanyRequest>(
        api, "/api/Companies", ClientJsonContext.Default.CompanyPage, ClientJsonContext.Default.Companies,
        ClientJsonContext.Default.CompanyDetailsDto, ClientJsonContext.Default.CreateCompanyRequest,
        ClientJsonContext.Default.UpdateCompanyRequest, x => x.Id, x => x.Name is not null,
        x => x.Id, x => !string.IsNullOrWhiteSpace(x.Name))
{
    public Task<TableCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync("/api/Companies/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) => CapabilitiesAsync(ct);
    public Task<LookupOption[]> SearchCitiesAsync(string text, CancellationToken ct = default) =>
        CorporateStarter.Client.Core.Lookups.LookupQuery.SearchAsync(Api, "/api/Companies/city-options", text, ct);
}
