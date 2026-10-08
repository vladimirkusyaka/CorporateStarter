using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;

public interface ICompanyTableRepository
{
    Task<PagedResult<CompanyListItemDto>> QueryAsync(TableRequest request, CancellationToken ct);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct);
    Task<LookupOption[]> SearchCitiesAsync(LookupRequest request, CancellationToken ct);
    Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct);
}
