using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Security;

public sealed partial class PermissionsController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.PermissionsRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(false, false, false, true, false));
    [HttpPost("query"), Authorize(Policy = AppPermissions.PermissionsRead)]
    public async Task<ActionResult<PagedResult<PermissionListItemDto>>> Query(TableRequest request,
        [FromServices] IPermissionTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.PermissionsRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] IPermissionTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.PermissionsRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] IPermissionTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
}