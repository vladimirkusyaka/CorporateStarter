using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;

public interface ICountryTableRepository
{
    Task<PagedResult<CountryListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
