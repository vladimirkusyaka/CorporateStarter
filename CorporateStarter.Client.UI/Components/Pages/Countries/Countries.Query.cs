using System.Text.Json;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CorporateStarter.Client.UI.Components.Pages.Countries;

public partial class Countries
{
    private enum CountryColumn { Code, Name, NativeName, PhoneCode, Status }
    private readonly Dictionary<CountryColumn, CountryColumnFilter> _filters = [];
    private CountryCapabilities _capabilities = new(false, false, false, false, false);
    private CountryColumn? _sortColumn;
    private bool _sortDescending, _querying;
    private int _totalCount;
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_totalCount / (double)RowsPerPage));
    private IEnumerable<CountryColumn> VisibleColumns => Enum.GetValues<CountryColumn>()
        .Where(x => x != CountryColumn.Status || _capabilities.ViewInactive);
    private string _searchValue = string.Empty;
    private Guid? _lastFoundId;
    private string? _searchMessage;
    private string _search
    {
        get => _searchValue;
        set { if (_searchValue == value) return; _searchValue = value; ResetSearchPosition(); }
    }
    private static string Field(CountryColumn column) => column == CountryColumn.Status ? "isActive" :
        char.ToLowerInvariant(column.ToString()[0]) + column.ToString()[1..];
    private static string ColumnLabel(CountryColumn column) => column switch
    {
        CountryColumn.NativeName => "Native name",
        CountryColumn.PhoneCode => "Phone code",
        _ => column.ToString()
    };
    private TableRequest BuildQuery() => new()
    {
        PageNumber = CurrentPage + 1,
        PageSize = RowsPerPage,
        Sorts = _sortColumn is { } column ? [new(Field(column), _sortDescending ? "desc" : "asc")] : [],
        Filters = _filters.Select(pair => pair.Key == CountryColumn.Status
            ? new TableFilter(Field(pair.Key), "in", Values: (pair.Value.Values ?? []).Select(x => JsonSerializer.SerializeToElement(x == "Active")).ToArray())
            : new TableFilter(Field(pair.Key), "contains", JsonSerializer.SerializeToElement(pair.Value.Text))).ToList()
    };
    private string SortAria(CountryColumn column) => _sortColumn != column ? "none" : _sortDescending ? "descending" : "ascending";
    private string SortArrow(CountryColumn column) => _sortColumn != column ? "" : _sortDescending ? "↓" : "↑";
    private string FilterTitle(CountryColumn column) => $"Filter {ColumnLabel(column)}" + (_filters.ContainsKey(column) ? " (active)" : "");
    private void ResetSearchPosition() { _lastFoundId = null; _searchMessage = null; }
    private void ResetViewPosition() { CurrentPage = 0; ClearSelection(); ResetSearchPosition(); }

    private async Task SortBy(CountryColumn column)
    {
        if (_disposed || _loading || _creating || _querying) return;
        _sortDescending = _sortColumn == column && !_sortDescending;
        _sortColumn = column;
        await ReloadAfterQueryChangeAsync(filterChanged: false);
    }

    private async Task ReloadAfterQueryChangeAsync(bool filterChanged)
    {
        _querying = true;
        try
        {
            ResetSearchPosition();
            if (filterChanged || _selectedIds.Count == 0) _currentPage = 0;

            // Find the selected row within the new filters and sort order.
            if (_selectedIds.Count == 1)
            {
                var found = await Client.FindAsync(new()
                {
                    Query = BuildQuery(),
                    LocateId = _selectedIds.Single()
                }, _lifetime.Token);
                if (_disposed) return;
                _currentPage = (found.PageNumber ?? 1) - 1;
            }

            await LoadAsync(preserveSelection: true, selectFirstIfMissing: filterChanged);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Reloading countries after a query change failed.");
            if (!_disposed)
            {
                ClearSelection();
                _countries = [];
                _error = "Could not load countries. Please retry.";
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
                _searchMessage = _filters.Count > 0 ? "No match in the filtered list. Try clearing the filters." : "No matching country found.";
                _lastFoundId = null; return;
            }
            CurrentPage = page - 1;
            await LoadAsync();
            if (_disposed) return;
            if (_countries.Any(x => x.Id == id))
            {
                SelectSavedCountry(id); _lastFoundId = id;
                _searchMessage = $"Found: {_countries.First(x => x.Id == id).Name}";
            }
            else { _searchMessage = "The list changed during search. Search again."; _lastFoundId = null; }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Country search failed."); _searchMessage = "Search is unavailable. Please retry."; }
        finally { _querying = false; }
    }
    private async Task LocateSavedAsync(Guid id)
    {
        _filters.Clear(); ResetViewPosition();
        try
        {
            var found = await Client.FindAsync(new() { Query = BuildQuery(), LocateId = id }, _lifetime.Token);
            if (_disposed) return;
            CurrentPage = (found.PageNumber ?? 1) - 1;
            await LoadAsync();
            if (!_disposed && _countries.Any(x => x.Id == id)) SelectSavedCountry(id);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Locating the saved country failed."); await LoadAsync(); }
    }
    private async Task ClearFilters()
    {
        if (_disposed || _loading || _creating || _querying) return;
        _filters.Clear(); await ReloadAfterQueryChangeAsync(filterChanged: true);
    }
    private async Task OpenFilterAsync(CountryColumn column)
    {
        if (_disposed || _loading || _creating || _querying) return;
        _creating = true;
        try
        {
            var options = column == CountryColumn.Status
                ? await Client.FilterValuesAsync(new() { Query = BuildQuery(), Field = Field(column) }, _lifetime.Token) : [];
            if (_disposed) return;
            var parameters = new DialogParameters<CountryFilterDialog>();
            parameters.Add(x => x.Label, ColumnLabel(column));
            parameters.Add(x => x.IsChoice, column == CountryColumn.Status);
            parameters.Add(x => x.Initial, _filters.GetValueOrDefault(column) ?? new CountryColumnFilter());
            parameters.Add(x => x.Options, options);
            _createDialog = await Dialogs.ShowAsync<CountryFilterDialog>($"Filter {ColumnLabel(column)}", parameters,
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
            if (_disposed || result is null || result.Canceled || result.Data is not CountryColumnFilter filter) return;
            if (filter.Values is null && string.IsNullOrWhiteSpace(filter.Text)) _filters.Remove(column);
            else _filters[column] = filter;
            await ReloadAfterQueryChangeAsync(filterChanged: true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Logger.LogWarning(ex, "Country filter failed."); if (!_disposed) Snackbar.Add("Could not load the filter. Please retry.", Severity.Warning); }
        finally { _createDialog = null; _creating = false; }
    }
}
