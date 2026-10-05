using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> where TColumn : struct, Enum
{
    private MudTable<TItem>? _table;
    private readonly TableSelection<Guid> _selection = new();
    private IReadOnlySet<Guid> _selectedIds => _selection.SelectedIds;
    private Guid? _pendingSelection;
    private bool CanEdit => !ReadOnly && OpenEditor is not null && _capabilities.CanUpdate && !_querying && !_loading && !_creating && !_disposed && _selectedIds.Count == 1;
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

    private string RowClass(TItem record, int index) =>
        _selectedIds.Contains(ItemId(record)) ? "reference-row-selected" : string.Empty;

    private void OnRowClick(TableRowClickEventArgs<TItem> args)
    {
        if (args.Item is not null) SelectRow(args.Item, args.MouseEventArgs);
    }

    private Task OnRowDoubleClickAsync(TItem record, MouseEventArgs mouse)
    {
        if (ReadOnly || OpenEditor is null || _loading || _querying || _creating || _disposed || !_capabilities.CanUpdate || mouse.Button != 0)
            return Task.CompletedTask;

        var id = ItemId(record);
        // Ignore events queued for a row that has disappeared after a refresh.
        if (!_items.Any(item => ItemId(item) == id)) return Task.CompletedTask;

        _pendingSelection = null;
        _selection.SelectOnly(id);
        return OpenEditAsync();
    }

    private void SelectRow(TItem record, MouseEventArgs mouse)
    {
        if (_loading || _querying || _creating || _disposed || mouse.Button != 0 || _table is null) return;
        var visible = _table.FilteredItems.Skip(_table.CurrentPage * _table.RowsPerPage)
            .Take(_table.RowsPerPage).Select(x => ItemId(x)).ToList();
        _selection.Select(ItemId(record), visible, mouse.ShiftKey, mouse.CtrlKey || mouse.MetaKey);
    }

    private void SelectSavedRow(Guid id) { _pendingSelection = id; StateHasChanged(); }

    private void ApplyPendingSelection()
    {
        if (_pendingSelection is not { } id || _loading || _table is null) return;
        _pendingSelection = null;
        var items = _items.ToList();
        var index = items.FindIndex(x => ItemId(x) == id);
        if (index < 0) return;

        _selection.SelectOnly(id);
        StateHasChanged();
    }
}