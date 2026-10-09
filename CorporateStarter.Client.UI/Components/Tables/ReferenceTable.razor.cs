using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;

namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> : IDisposable where TColumn : struct, Enum
{
    [Parameter, EditorRequired] public ITableClient<TItem> Client { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<TableColumn<TItem, TColumn>> Columns { get; set; } = [];
    [Parameter, EditorRequired] public Func<TItem, Guid> ItemId { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TItem, string> ItemName { get; set; } = default!;
    [Parameter] public Func<Guid?, Task<MudBlazor.IDialogReference>>? OpenEditor { get; set; }
    [Parameter] public Func<object?, Guid?> SavedId { get; set; } = _ => null;
    [Parameter] public Func<TItem, Task<MudBlazor.IDialogReference>>? OpenDetails { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public Func<TItem, bool> CanEditItem { get; set; } = _ => true;
    [Parameter] public Func<TItem, bool> CanDeleteItem { get; set; } = _ => true;
    [Parameter] public string Title { get; set; } = "Records";
    [Parameter] public string Singular { get; set; } = "record";
    private readonly string _elementId = "table-" + Guid.NewGuid().ToString("N");

    [Inject]
    private ILogger<ReferenceTable<TItem, TColumn>> Logger { get; set; } = default!;

    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<TItem> _items = [];
    private bool _loadStarted;
    private bool _hasLoaded;
    private bool _loading;
    private bool _disposed;
    private string? _error;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await LoadAsync();
        else
            ApplyPendingSelection();
    }

    protected Task LoadAsync() => LoadAsync(preserveSelection: false);

    private async Task LoadAsync(bool preserveSelection, bool selectFirstIfMissing = false)
    {
        if (_disposed || _loading)
            return;

        var cancellationToken = _lifetime.Token;
        if (!preserveSelection) ClearSelection();
        else _pendingSelection = null;
        _pageNumberDraft = null;
        ResetSearchPosition();
        _loadStarted = true;
        _loading = true;
        _error = null;
        // Keep the displayed page mounted until its replacement arrives.
        StateHasChanged();

        try
        {
            _capabilities = await Client.GetCapabilitiesAsync(cancellationToken);
            if (!_capabilities.ViewInactive)
                foreach (var column in Columns.Where(x => x.IsStatus)) _queryState.RemoveColumn(column.Key);
            var page = await Client.QueryAsync(BuildQuery(), cancellationToken);
            if (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                _items = page.Items;
                _queryState.ApplyPage(page.Page, page.TotalCount);
                _hasLoaded = true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ClientApiHttpException exception)
        {
            _error = exception.StatusCode switch
            {
                401 => "The request could not be authorized. Retry after session verification.",
                403 => "You do not have permission to view records.",
                404 => "The records service could not be found.",
                429 => "Too many requests. Please wait before retrying.",
                >= 500 => "The records service is temporarily unavailable. Please retry.",
                _ => "The records service rejected the request. Please retry."
            };
        }
        catch (ClientApiSessionException exception)
        {
            _error = exception.Failure switch
            {
                ClientApiSessionFailure.Busy =>
                    "Session verification is in progress. Retry when it finishes.",
                ClientApiSessionFailure.SessionChanged =>
                    "The session changed. Reload the record list.",
                _ => "A valid sign-in is required to load records."
            };
        }
        catch (ClientApiTransportException)
        {
            _error = "Could not receive a response from the service. Please retry.";
        }
        catch (InvalidDataException)
        {
            Logger.LogWarning("The records service returned an invalid response.");
            _error = "The service returned an unexpected response. Please contact support if this persists.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading the record list failed.");
            _error = "Could not load records. Please retry.";
        }
        finally
        {
            if (_error is not null)
            {
                // A failed refresh must not leave stale rows actionable (including lost access).
                _items = [];
                _hasLoaded = false;
                ClearSelection();
            }
            if (preserveSelection)
                _selection.RetainVisible(_disposed ? Array.Empty<Guid>() : _items.Select(x => ItemId(x)),
                    selectFirstIfEmpty: selectFirstIfMissing && _error is null);
            _loading = false;
            if (!_disposed)
                StateHasChanged();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _createDialog?.Close();
        _lifetime.Cancel();
        _lifetime.Dispose();
        _items = [];
        _error = null;
        GC.SuppressFinalize(this);
    }
}
