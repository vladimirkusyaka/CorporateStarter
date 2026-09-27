using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;

public interface IPositionTableRepository
{
    Task<PagedResult<PositionListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
