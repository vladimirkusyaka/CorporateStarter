using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security;

public interface IUserTableRepository
{
    Task<PagedResult<UserTableItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
