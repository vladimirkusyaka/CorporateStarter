using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using static MudBlazor.CategoryTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> where TColumn : struct, Enum
{
    private readonly TableQueryState<TColumn, TableColumnFilter> _queryState = new();
    private IReadOnlyDictionary<TColumn, TableColumnFilter> _filters => _queryState.Filters;
    private TableCapabilities _capabilities = new(false, false, false, false, false);
    private bool _querying;
    private int _totalCount => _queryState.TotalCount;
    private int TotalPages => _queryState.TotalPages;
    private IEnumerable<TColumn> VisibleColumns => Columns.Where(x => !x.IsStatus || _capabilities.ViewInactive).Select(x => x.Key);
    private string _searchValue = string.Empty;
    private Guid? _lastFoundId;
    private string? _searchMessage;
    private string _search
    {
        get => _searchValue;
        set { if (_searchValue == value) return; _searchValue = value; ResetSearchPosition(); }
    }
    private TableColumn<TItem, TColumn> Definition(TColumn column) => Columns.Single(x => EqualityComparer<TColumn>.Default.Equals(x.Key, column));
    private string Field(TColumn column) => Definition(column).Field;
    private string ColumnLabel(TColumn column) => Definition(column).Label;
    private TableRequest BuildQuery() => _queryState.BuildRequest(Field, SerializeFilter);
    private TableFilter SerializeFilter(TColumn column, TableColumnFilter filter) =>
        Definition(column).IsStatus
            ? new TableFilter(Field(column), "in", Values: (filter.Values ?? [])
                .Select(x => JsonSerializer.SerializeToElement(x == "Active")).ToArray())
            : new TableFilter(Field(column), "contains", JsonSerializer.SerializeToElement(filter.Text));
    private string SortAria(TColumn column) => !EqualityComparer<TColumn?>.Default.Equals(_queryState.SortColumn, column) ? "none" : _queryState.SortDescending ? "descending" : "ascending";
    private string SortArrow(TColumn column) => !EqualityComparer<TColumn?>.Default.Equals(_queryState.SortColumn, column) ? "" : _queryState.SortDescending ? "↓" : "↑";
    private string FilterTitle(TColumn column) => $"Filter {ColumnLabel(column)}" + (_filters.ContainsKey(column) ? " (active)" : "");
    private void ResetSearchPosition() { _lastFoundId = null; _searchMessage = null; }
    private void ResetViewPosition() { CurrentPage = 0; ClearSelection(); ResetSearchPosition(); }

    private async Task SortBy(TColumn column)
    {
        if (_disposed || _loading || _creating || _querying) return;
        _queryState.ToggleSort(column);
        await ReloadAfterQueryChangeAsync(filterChanged: false);
    }

    private async Task ReloadAfterQueryChangeAsync(bool filterChanged)
    {
        _querying = true;
        try
        {
            ResetSearchPosition();
            if (filterChanged || _selectedIds.Count == 0) _queryState.PageIndex = 0;

            // Find the selected row within the new filters and sort order.
            if (_selectedIds.Count == 1)
            {
                var found = await Client.FindAsync(new()
                {
                    Query = BuildQuery(),
                    LocateId = _selectedIds.Single()
                }, _lifetime.Token);
                if (_disposed) return;
                _queryState.PageIndex = (found.PageNumber ?? 1) - 1;
            }

            await LoadAsync(preserveSelection: true, selectFirstIfMissing: filterChanged);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Reloading records after a query change failed.");
            if (!_disposed)
            {
                ClearSelection();
                _items = [];
                _error = "Could not load records. Please retry.";
            }
        }
        finally { _querying = false; }
    }
    private Task PreviousPageAsync() => GoToPageAsync(CurrentPage - 1);
    private Task NextPageAsync() => GoToPageAsync(CurrentPage + 1);
    private async Task GoToPageAsync(int page)
    {
        if (_disposed || _loading || _creating || _querying || page < 0 || page >= TotalPages) return;
        CurrentPage = page;
        await LoadAsync();
    }
    private async Task ChangePageSizeAsync(ChangeEventArgs args)
    {
        if (_disposed || _loading || _creating || _querying || !int.TryParse(args.Value?.ToString(), out var size) || !PageSizes.Contains(size)) return;
        RowsPerPage = size;
        ResetViewPosition();
        await LoadAsync();
    }
    private async Task FindNext()
    {
        if (_disposed || _loading || _creating || _querying || string.IsNullOrWhiteSpace(_search)) return;
        _querying = true;
        try
        {
            var found = await Client.FindAsync(new() { Query = BuildQuery(), Text = _search.Trim(), AfterId = _lastFoundId }, _lifetime.Token);
            if (_disposed) return;
            if (found.Id is not { } id || found.PageNumber is not { } page)
            {
                _searchMessage = _filters.Count > 0 ? "No match in the filtered list. Try clearing the filters." : "No matching record found.";
                _lastFoundId = null; return;
            }
            CurrentPage = page - 1;
            await LoadAsync();
            if (_disposed) return;
            if (_items.Any(x => ItemId(x) == id))
            {
                SelectSavedRow(id); _lastFoundId = id;
                _searchMessage = $"Found: {ItemName(_items.First(x => ItemId(x) == id))}";
            }
            else { _searchMessage = "The list changed during search. Search again."; _lastFoundId = null; }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Record search failed."); _searchMessage = "Search is unavailable. Please retry."; }
        finally { _querying = false; }
    }
    private async Task LocateSavedAsync(Guid id)
    {
        _queryState.ClearFilters(); ResetViewPosition();
        try
        {
            var found = await Client.FindAsync(new() { Query = BuildQuery(), LocateId = id }, _lifetime.Token);
            if (_disposed) return;
            CurrentPage = (found.PageNumber ?? 1) - 1;
            await LoadAsync();
            if (!_disposed && _items.Any(x => ItemId(x) == id)) SelectSavedRow(id);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Locating the saved record failed."); await LoadAsync(); }
    }
    private async Task ClearFilters()
    {
        if (_disposed || _loading || _creating || _querying) return;
        _queryState.ClearFilters(); await ReloadAfterQueryChangeAsync(filterChanged: true);
    }
    private async Task OpenFilterAsync(TColumn column)
    {
        if (_disposed || _loading || _creating || _querying) return;
        _creating = true;
        try
        {
            var options = Definition(column).IsStatus
                ? await Client.FilterValuesAsync(new() { Query = BuildQuery(), Field = Field(column) }, _lifetime.Token) : [];
            if (_disposed) return;
            var parameters = new DialogParameters<TableFilterDialog>();
            parameters.Add(x => x.Label, ColumnLabel(column));
            parameters.Add(x => x.IsChoice, Definition(column).IsStatus);
            parameters.Add(x => x.Initial, _filters.GetValueOrDefault(column) ?? new TableColumnFilter());
            parameters.Add(x => x.Options, options);
            _createDialog = await Dialogs.ShowAsync<TableFilterDialog>($"Filter {ColumnLabel(column)}", parameters,
                new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                    FullWidth = true,
                    BackdropClick = false,
                    CloseButton = false,
                    CloseOnEscapeKey = true,
                    CloseOnNavigation = false
                });
            if (_disposed) { _createDialog.Close(); return; }
            var result = await _createDialog.Result;
            if (_disposed || result is null || result.Canceled || result.Data is not TableColumnFilter filter) return;
            if (filter.Values is null && string.IsNullOrWhiteSpace(filter.Text)) _queryState.RemoveFilter(column);
            else _queryState.SetFilter(column, filter);
            await ReloadAfterQueryChangeAsync(filterChanged: true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Record filter failed."); if (!_disposed) Snackbar.Add("Could not load the filter. Please retry.", Severity.Warning); }
        finally { _createDialog = null; _creating = false; }
    }
}