using CorporateStarter.Shared.Common;
namespace CorporateStarter.Client.Core.Tables;

public interface ITableClient<TItem>
{
    Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default);
    Task<PagedResult<TItem>> QueryAsync(TableRequest request, CancellationToken ct = default);
    Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct = default);
    Task<string[]> FilterValuesAsync(TableValuesRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
