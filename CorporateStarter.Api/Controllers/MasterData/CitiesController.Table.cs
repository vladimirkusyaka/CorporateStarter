using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.MasterData;

public sealed partial class CitiesController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.CitiesRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(
        User.HasClaim("permission", AppPermissions.CitiesCreate),
        User.HasClaim("permission", AppPermissions.CitiesUpdate),
        User.HasClaim("permission", AppPermissions.CitiesDelete),
        true, User.HasClaim("permission", AppPermissions.CitiesUpdate)));
    [HttpPost("query"), Authorize(Policy = AppPermissions.CitiesRead)]
    public async Task<ActionResult<PagedResult<CityListItemDto>>> Query(TableRequest request,
        [FromServices] ICityTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.CitiesRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] ICityTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.CitiesRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] ICityTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
    // Country identity is needed to edit a city; this does not expose the Countries CRUD API.
    [HttpPost("country-options"), Authorize(Policy = AppPermissions.CitiesRead)]
    public async Task<ActionResult<LookupOption[]>> CountryOptions(LookupRequest request,
        [FromServices] ICityTableRepository repository, CancellationToken ct)
    {
        if (!User.HasClaim("permission", AppPermissions.CitiesCreate) &&
            !User.HasClaim("permission", AppPermissions.CitiesUpdate)) return Forbid();
        return Ok(await repository.SearchCountriesAsync(request, ct));
    }
}