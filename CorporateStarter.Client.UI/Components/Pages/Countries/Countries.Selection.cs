using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using CorporateStarter.Client.Core.Tables;

namespace CorporateStarter.Client.UI.Components.Pages.Countries;

public partial class Countries
{
    private MudTable<CountryListItemDto>? _table;
    private readonly TableSelection<Guid> _selection = new();
    private IReadOnlySet<Guid> _selectedIds => _selection.SelectedIds;
    private Guid? _pendingSelection;
    private bool CanEdit => _capabilities.CanUpdate && !_querying && !_loading && !_creating && !_disposed && _selectedIds.Count == 1;
    private int CurrentPage
    {
        get => _queryState.PageIndex;
        set { if (_queryState.PageIndex == value) return; _queryState.PageIndex = value; ClearSelection(); }
    }
    private int RowsPerPage
    {
        get => _queryState.PageSize;
        set { if (_queryState.PageSize == value) return; _queryState.PageSize = value; ClearSelection(); }
    }

    private void ClearSelection()
    {
        _selection.Clear();
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
        _selection.Select(country.Id, visible, mouse.ShiftKey, mouse.CtrlKey);
    }

    private void SelectSavedCountry(Guid id) { _pendingSelection = id; StateHasChanged(); }

    private void ApplyPendingSelection()
    {
        if (_pendingSelection is not { } id || _loading || _table is null) return;
        _pendingSelection = null;
        var items = _countries.ToList();
        var index = items.FindIndex(x => x.Id == id);
        if (index < 0) return;

        _selection.SelectOnly(id);
        StateHasChanged();
    }
}

