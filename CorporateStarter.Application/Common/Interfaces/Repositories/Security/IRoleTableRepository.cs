using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security;

public interface IRoleTableRepository
{
    Task<PagedResult<RoleListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
