using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Charts;

namespace CorporateStarter.Web.Components.Pages.Countries;

public partial class Countries
{
    private MudTable<CountryListItemDto>? _table;
    private readonly HashSet<Guid> _selectedIds = [];
    private Guid? _selectionAnchor;
    private Guid? _pendingSelection;
    private int _currentPage;
    private int _rowsPerPage = 20;
    private bool CanEdit => _capabilities.CanUpdate && !_querying && !_loading && !_creating && !_disposed && _selectedIds.Count == 1;
    private int CurrentPage
    {
        get => _currentPage;
        set { if (_currentPage == value) return; _currentPage = value; ClearSelection(); }
    }
    private int RowsPerPage
    {
        get => _rowsPerPage;
        set { if (_rowsPerPage == value) return; _rowsPerPage = value; ClearSelection(); }
    }

    private void ClearSelection()
    {
        _selectedIds.Clear();
        _selectionAnchor = null;
        _pendingSelection = null;
    }

    private string CountryRowClass(CountryListItemDto country, int index) =>
        _selectedIds.Contains(country.Id) ? "country-row-selected" : string.Empty;

    private void OnCountryRowClick(TableRowClickEventArgs<CountryListItemDto> args)
    {
        if (args.Item is not null) SelectCountry(args.Item, args.MouseEventArgs);
    }

    private void SelectCountry(CountryListItemDto country, MouseEventArgs mouse)
    {
        if (_loading || _creating || _disposed || mouse.Button != 0 || _table is null) return;
        var visible = _table.FilteredItems.Skip(_table.CurrentPage * _table.RowsPerPage)
            .Take(_table.RowsPerPage).Select(x => x.Id).ToList();
        var clicked = visible.IndexOf(country.Id);
        if (clicked < 0) return;
        var anchor = _selectionAnchor is { } id ? visible.IndexOf(id) : -1;
        _selectedIds.Clear();
        if (mouse.ShiftKey && anchor >= 0)
        {
            for (var i = Math.Min(anchor, clicked); i <= Math.Max(anchor, clicked); i++)
                _selectedIds.Add(visible[i]);
        }
        else
        {
            _selectedIds.Add(country.Id);
            _selectionAnchor = country.Id;
        }
    }

    private void SelectSavedCountry(Guid id) { _pendingSelection = id; StateHasChanged(); }

    private void ApplyPendingSelection()
    {
        if (_pendingSelection is not { } id || _loading || _table is null) return;
        _pendingSelection = null;
        var items = _countries.ToList();
        var index = items.FindIndex(x => x.Id == id);
        if (index < 0) return;

        _selectedIds.Clear();
        _selectedIds.Add(id);
        _selectionAnchor = id;
        StateHasChanged();
    }
}
