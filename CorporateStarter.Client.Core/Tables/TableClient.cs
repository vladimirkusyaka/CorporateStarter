using System.Text.Json.Serialization.Metadata;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Common;
namespace CorporateStarter.Client.Core.Tables;

public abstract class TableClient<TItem>(ClientApiClient api, string route,
    JsonTypeInfo<PagedResult<TItem>> pageType, Func<TItem, Guid> id) : ITableClient<TItem>
{
    protected ClientApiClient Api { get; } = api;
    protected string Route { get; } = route;
    protected Func<TItem, Guid> ItemId { get; } = id;
    public abstract Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default);
    public async Task<PagedResult<TItem>> QueryAsync(TableRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var page = await Api.PostJsonAsync(Route + "/query", request, ClientJsonContext.Default.TableRequest,
            pageType, ct).ConfigureAwait(false);
        if (page.Items is null || page.Page < 1 || page.PageSize != request.PageSize || page.TotalCount < 0 ||
            page.Items.Count > page.PageSize || page.Items.Any(x => x is null || ItemId(x) == Guid.Empty) ||
            page.Items.Select(ItemId).Distinct().Count() != page.Items.Count)
            throw new InvalidDataException("The server returned an invalid table page.");
        return page;
    }
    public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct = default) =>
        Api.PostJsonAsync(Route + "/find", request, ClientJsonContext.Default.TableFindRequest,
            ClientJsonContext.Default.TableFindResult, ct);
    public Task<string[]> FilterValuesAsync(TableValuesRequest request, CancellationToken ct = default) =>
        Api.PostJsonAsync(Route + "/filter-values", request, ClientJsonContext.Default.TableValuesRequest,
            ClientJsonContext.Default.StringArray, ct);
    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Record ID is required.", nameof(id));
        return Api.DeleteAsync($"{Route}/{id:D}", ct);
    }
}
