using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Permissions;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security;

public interface IPermissionTableRepository
{
    Task<PagedResult<PermissionListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
