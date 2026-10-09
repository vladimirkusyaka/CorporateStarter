using CorporateStarter.Api.Controllers;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CorporateStarter.Api.Controllers.Audit;

[TableRequestErrors]
public partial class AuditController
{
    [HttpGet("capabilities"), Authorize(Policy = AppPermissions.AuditRead)]
    public ActionResult<TableCapabilities> Capabilities() => Ok(new TableCapabilities(false, false, false, false, false));
    [HttpPost("query"), Authorize(Policy = AppPermissions.AuditRead)]
    public async Task<ActionResult<PagedResult<ChangeListItemDto>>> Query(TableRequest request, [FromServices] IChangeTableRepository repository, CancellationToken ct)
        => Ok(await repository.QueryAsync(request, ct));
    [HttpPost("find"), Authorize(Policy = AppPermissions.AuditRead)]
    public async Task<ActionResult<TableFindResult>> Find(TableFindRequest request, [FromServices] IChangeTableRepository repository, CancellationToken ct)
        => Ok(await repository.FindAsync(request, ct));
    [HttpPost("filter-values"), Authorize(Policy = AppPermissions.AuditRead)]
    public async Task<ActionResult<string[]>> Values(TableValuesRequest request, [FromServices] IChangeTableRepository repository, CancellationToken ct)
        => Ok(await repository.ValuesAsync(request, ct));
    [HttpGet("{id:guid}"), Authorize(Policy = AppPermissions.AuditRead)]
    public async Task<ActionResult<AuditLogListItemDto>> Details(Guid id, [FromServices] IChangeTableRepository repository, CancellationToken ct)
    {
        var item = await repository.GetDetailsAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }
}
