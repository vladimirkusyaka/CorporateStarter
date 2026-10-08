using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Directory;

public sealed partial class CompaniesController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.CompaniesRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(
        User.HasClaim("permission", AppPermissions.CompaniesCreate),
        User.HasClaim("permission", AppPermissions.CompaniesUpdate),
        User.HasClaim("permission", AppPermissions.CompaniesDelete),
        true, User.HasClaim("permission", AppPermissions.CompaniesUpdate)));
    [HttpPost("query"), Authorize(Policy = AppPermissions.CompaniesRead)]
    public async Task<ActionResult<PagedResult<CompanyListItemDto>>> Query(TableRequest request,
        [FromServices] ICompanyTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.CompaniesRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] ICompanyTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.CompaniesRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] ICompanyTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
    // City options belong to the company editor; Cities.Read is not additionally required.
    [HttpPost("city-options"), Authorize(Policy = AppPermissions.CompaniesRead)]
    public async Task<ActionResult<LookupOption[]>> CityOptions(LookupRequest request,
        [FromServices] ICompanyTableRepository repository, CancellationToken ct)
    {
        if (!User.HasClaim("permission", AppPermissions.CompaniesCreate) &&
            !User.HasClaim("permission", AppPermissions.CompaniesUpdate)) return Forbid();
        return Ok(await repository.SearchCitiesAsync(request, ct));
    }
}