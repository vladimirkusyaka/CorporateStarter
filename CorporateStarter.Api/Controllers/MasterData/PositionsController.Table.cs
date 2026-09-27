using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.MasterData;

public sealed partial class PositionsController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.PositionsRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(
        User.HasClaim("permission", AppPermissions.PositionsCreate),
        User.HasClaim("permission", AppPermissions.PositionsUpdate),
        User.HasClaim("permission", AppPermissions.PositionsDelete),
        true, User.HasClaim("permission", AppPermissions.PositionsUpdate)));
    [HttpPost("query"), Authorize(Policy = AppPermissions.PositionsRead)]
    public async Task<ActionResult<PagedResult<PositionListItemDto>>> Query(TableRequest request,
        [FromServices] IPositionTableRepository repository, CancellationToken ct) => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.PositionsRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request,
        [FromServices] IPositionTableRepository repository, CancellationToken ct) => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.PositionsRead)]
    public async Task<ActionResult<string[]>> FilterValues(TableValuesRequest request,
        [FromServices] IPositionTableRepository repository, CancellationToken ct) => Ok(await repository.ValuesAsync(request, ct));
}