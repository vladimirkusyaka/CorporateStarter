using System.Text.Json.Serialization.Metadata;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Shared.Common;
namespace CorporateStarter.Client.Core.Tables;

public abstract class MasterDataClient<TItem, TDetails, TCreate, TUpdate>(
    ClientApiClient api, string route, JsonTypeInfo<PagedResult<TItem>> pageType,
    JsonTypeInfo<TItem[]> listType, JsonTypeInfo<TDetails> detailsType,
    JsonTypeInfo<TCreate> createType, JsonTypeInfo<TUpdate> updateType,
    Func<TItem, Guid> itemId, Func<TItem, bool> validItem,
    Func<TDetails, Guid> detailsId, Func<TDetails, bool> validDetails)
    : TableClient<TItem>(api, route, pageType, itemId)
{
    public async Task<TDetails> CreateAsync(TCreate request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await Api.PostJsonAsync(Route, request, createType, detailsType, cancellationToken).ConfigureAwait(false);
        Validate(result);
        return result;
    }
    public async Task<TDetails> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequireId(id);
        var result = await Api.GetJsonAsync($"{Route}/{id:D}", detailsType, cancellationToken).ConfigureAwait(false);
        Validate(result, id);
        return result;
    }
    public async Task<TDetails> UpdateAsync(Guid id, TUpdate request, CancellationToken cancellationToken = default)
    {
        RequireId(id);
        ArgumentNullException.ThrowIfNull(request);
        var result = await Api.PutJsonAsync($"{Route}/{id:D}", request, updateType, detailsType, cancellationToken).ConfigureAwait(false);
        Validate(result, id);
        return result;
    }
    public async Task<IReadOnlyList<TItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await Api.GetJsonAsync(Route, listType, cancellationToken).ConfigureAwait(false);
        if (items.Any(x => x is null || ItemId(x) == Guid.Empty || !validItem(x)))
            throw new InvalidDataException("The API returned an invalid list item.");
        return items;
    }
    private void Validate(TDetails value, Guid? expectedId = null)
    {
        if (value is null || detailsId(value) == Guid.Empty ||
            (expectedId is { } id && detailsId(value) != id) || !validDetails(value))
            throw new InvalidDataException("The API returned invalid record details.");
    }
    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Record ID is required.", nameof(id));
    }
}
