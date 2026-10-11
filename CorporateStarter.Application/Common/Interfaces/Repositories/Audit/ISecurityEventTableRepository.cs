using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Audit;

public interface ISecurityEventTableRepository
{
    Task<PagedResult<SecurityEventRowDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
    Task<SecurityEventListItemDto?> GetDetailsAsync(Guid id, CancellationToken ct);
}
