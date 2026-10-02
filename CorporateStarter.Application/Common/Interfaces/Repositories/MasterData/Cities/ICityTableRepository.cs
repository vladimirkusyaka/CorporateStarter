using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;

public interface ICityTableRepository
{
    Task<PagedResult<CityListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<LookupOption[]> SearchCountriesAsync(LookupRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
