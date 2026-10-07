using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Security;

public sealed partial class UsersController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.UsersRead)]
    public ActionResult<UserCapabilities> Capabilities() => Ok(new UserCapabilities(
        new TableCapabilities(User.HasClaim("permission", AppPermissions.UsersCreate),
            User.HasClaim("permission", AppPermissions.UsersUpdate),
            User.HasClaim("permission", AppPermissions.UsersDelete), true,
            User.HasClaim("permission", AppPermissions.UsersUpdate)),
        User.HasClaim("permission", AppPermissions.UsersChangePassword)));

    // Options belong to the user editor, so Roles.Read is not additionally required.
    [HttpGet("role-options"), Authorize(Policy = AppPermissions.UsersRead)]
    public async Task<IActionResult> RoleOptions(
        [FromServices] IRoleReadRepository repository, CancellationToken ct)
    {
        if (!User.HasClaim("permission", AppPermissions.UsersCreate) &&
            !User.HasClaim("permission", AppPermissions.UsersUpdate)) return Forbid();
        return Ok(await repository.GetListAsync(ct));
    }

    [HttpPost("query"), Authorize(Policy = AppPermissions.UsersRead)]
    public async Task<ActionResult<PagedResult<UserTableItemDto>>> Query(TableRequest request,
        [FromServices] IUserTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.UsersRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] IUserTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.UsersRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] IUserTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
}