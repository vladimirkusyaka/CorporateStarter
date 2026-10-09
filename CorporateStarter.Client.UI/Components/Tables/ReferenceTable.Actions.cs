using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> where TColumn : struct, Enum
{
    private bool CanView => OpenDetails is not null && !_disposed && !_loading && !_querying && !_creating && _selectedIds.Count == 1 && _items.Any(x => _selectedIds.Contains(ItemId(x)));
    private async Task OpenDetailsAsync()
    {
        if (!CanView) return;
        var item = _items.Single(x => _selectedIds.Contains(ItemId(x)));
        _creating = true;
        try
        {
            _createDialog = await OpenDetails!(item);
            if (_disposed) { _createDialog.Close(); return; }
            await _createDialog.Result;
        }
        finally { _createDialog = null; _creating = false; }
    }
    [Parameter] public IReadOnlyList<TableRecordAction<TItem>> Actions { get; set; } = [];
    private bool CanRunAction(TableRecordAction<TItem> action) => !ReadOnly && !_disposed &&
        !_loading && !_querying && !_creating && _selectedIds.Count == 1 &&
        _items.Any(item => _selectedIds.Contains(ItemId(item)) && action.CanExecute(item));

    private async Task RunActionAsync(TableRecordAction<TItem> action)
    {
        if (!CanRunAction(action)) return;
        var item = _items.Single(x => _selectedIds.Contains(ItemId(x)));
        _creating = true;
        try
        {
            _createDialog = await action.Open(item);
            if (_disposed) { _createDialog.Close(); return; }
            var result = await _createDialog.Result;
            if (!_disposed && result is { Canceled: false }) await LoadAsync(preserveSelection: true);
        }
        finally { _createDialog = null; _creating = false; }
    }
}
