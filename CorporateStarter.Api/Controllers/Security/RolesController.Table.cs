using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Security;

public sealed partial class RolesController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.RolesRead)]
    public ActionResult<RoleCapabilities> Capabilities() => Ok(new RoleCapabilities(
        new TableCapabilities(User.HasClaim("permission", AppPermissions.RolesCreate),
            User.HasClaim("permission", AppPermissions.RolesUpdate),
            User.HasClaim("permission", AppPermissions.RolesDelete), true,
            User.HasClaim("permission", AppPermissions.RolesUpdate)),
        User.HasClaim("permission", AppPermissions.RolesManagePermissions)));

    [HttpGet("permission-options"), Authorize(Policy = AppPermissions.RolesRead),
        Authorize(Policy = AppPermissions.RolesManagePermissions)]
    public async Task<IActionResult> PermissionOptions(
        [FromServices] IPermissionReadRepository repository, CancellationToken ct) =>
        Ok(await repository.GetListAsync(ct));

    [HttpPost("query"), Authorize(Policy = AppPermissions.RolesRead)]
    public async Task<ActionResult<PagedResult<RoleListItemDto>>> Query(TableRequest request,
        [FromServices] IRoleTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.RolesRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] IRoleTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.RolesRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] IRoleTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
}