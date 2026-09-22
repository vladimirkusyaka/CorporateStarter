using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.MasterData;

public sealed partial class CountriesController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.CountriesRead)]
    public ActionResult<CountryCapabilities> Capabilities([FromServices] ICountryAccess access) => Ok(access.Capabilities);
    [HttpPost("query"), Authorize(Policy = AppPermissions.CountriesRead)]
    public async Task<ActionResult<PagedResult<CountryListItemDto>>> Query(TableRequest request,
        [FromServices] ICountryTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.CountriesRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] ICountryTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.CountriesRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] ICountryTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
}
