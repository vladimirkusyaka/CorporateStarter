using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Directory;

public sealed partial class PersonsController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.PersonsRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(
        User.HasClaim("permission", AppPermissions.PersonsCreate),
        User.HasClaim("permission", AppPermissions.PersonsUpdate),
        User.HasClaim("permission", AppPermissions.PersonsDelete),
        true, User.HasClaim("permission", AppPermissions.PersonsUpdate)));
    [HttpPost("query"), Authorize(Policy = AppPermissions.PersonsRead)]
    public async Task<ActionResult<PagedResult<PersonListItemDto>>> Query(TableRequest request,
        [FromServices] IPersonTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.PersonsRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] IPersonTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.PersonsRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] IPersonTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
    [HttpPost("company-options"), Authorize(Policy = AppPermissions.PersonsRead)]
    public async Task<ActionResult<LookupOption[]>> CompanyOptions(LookupRequest request,
        [FromServices] IPersonTableRepository repository, CancellationToken ct)
    {
        if (!CanEditPersons) return Forbid();
        return Ok(await repository.SearchCompaniesAsync(request, ct));
    }
    [HttpPost("position-options"), Authorize(Policy = AppPermissions.PersonsRead)]
    public async Task<ActionResult<LookupOption[]>> PositionOptions(LookupRequest request,
        [FromServices] IPersonTableRepository repository, CancellationToken ct)
    {
        if (!CanEditPersons) return Forbid();
        return Ok(await repository.SearchPositionsAsync(request, ct));
    }
    private bool CanEditPersons => User.HasClaim("permission", AppPermissions.PersonsCreate) ||
        User.HasClaim("permission", AppPermissions.PersonsUpdate);
}
