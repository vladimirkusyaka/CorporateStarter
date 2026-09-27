using Microsoft.AspNetCore.Components;
using MudBlazor;
using CorporateStarter.Shared.Common;

namespace CorporateStarter.Client.UI.Components.Tables;

public partial class ReferenceTable<TItem, TColumn> where TColumn : struct, Enum
{
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    private bool _creating;
    private IDialogReference? _createDialog;

    private Task OpenCreateAsync() => OpenEditorAsync(null);
    private Task OpenEditAsync() => CanEdit ? OpenEditorAsync(_selectedIds.Single()) : Task.CompletedTask;

    private async Task OpenEditorAsync(Guid? recordId)
    {
        if (_creating || _disposed || _loading || _querying || (recordId.HasValue ? !_capabilities.CanUpdate : !_capabilities.CanCreate)) return;
        _creating = true;
        try
        {
            _createDialog = await OpenEditor(recordId);
            if (_disposed) { _createDialog.Close(); return; }
            var result = await _createDialog.Result;
            if (_disposed || result is null || result.Canceled) return;
            if (SavedId(result.Data) is not null)
            {
                _search = string.Empty;
                Snackbar.Add(recordId.HasValue ? "Record updated." : "Record created.", Severity.Success);
            }
            if (SavedId(result.Data) is { } savedId) await LocateSavedAsync(savedId);
            else await LoadAsync();
        }
        finally
        {
            _createDialog = null;
            _creating = false;
        }
    }
}
