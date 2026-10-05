using CorporateStarter.Shared.Common;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> where TColumn : struct, Enum
{
    // The trash icon from the design system.
    private const string DeleteIcon = "<g fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M3 6h18'/><path d='M8 6V4a1 1 0 011-1h6a1 1 0 011 1v2'/><path d='M6 6l1 14a1 1 0 001 1h8a1 1 0 001-1l1-14'/></g>";
    private bool CanDelete => !ReadOnly && _capabilities.CanDelete && !_querying && !_loading && !_creating && !_disposed && _selectedIds.Count > 0;

    private async Task OpenDeleteAsync()
    {
        if (!CanDelete) return;
        var selected = _items.Where(x => _selectedIds.Contains(ItemId(x))).ToArray();
        if (selected.Length == 0) return;
        _creating = true; // Shared dialog guard for Add, Edit and Delete.
        try
        {
            var parameters = new DialogParameters<TableDeleteDialog>();
            parameters.Add(x => x.Items, selected.Select(x => new TableDeleteItem(ItemId(x), ItemName(x))).ToArray());
            parameters.Add(x => x.Delete, new Func<Guid, CancellationToken, Task>(Client.DeleteAsync));
            parameters.Add(x => x.Singular, Singular);
            parameters.Add(x => x.Plural, Title);
            _createDialog = await Dialogs.ShowAsync<TableDeleteDialog>($"Delete {Title}", parameters,
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    BackdropClick = false,
                    CloseButton = false,
                    CloseOnEscapeKey = false,
                    CloseOnNavigation = false
                });
            if (_disposed) { _createDialog.Close(); return; }
            var result = await _createDialog.Result;
            if (_disposed || result is null || result.Canceled) return;
            if (result.Data is int count)
                Snackbar.Add(count == 1 ? "Record deleted (set to inactive)." :
                    $"{count} records deleted (set to inactive).", Severity.Success);
            await LoadAsync();
        }
        finally
        {
            _createDialog = null;
            _creating = false;
        }
    }
}
