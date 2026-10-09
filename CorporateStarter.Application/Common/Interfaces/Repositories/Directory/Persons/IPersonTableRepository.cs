using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;

public interface IPersonTableRepository
{
    Task<PagedResult<PersonListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<LookupOption[]> SearchCompaniesAsync(LookupRequest request, CancellationToken ct);
    Task<LookupOption[]> SearchPositionsAsync(LookupRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
