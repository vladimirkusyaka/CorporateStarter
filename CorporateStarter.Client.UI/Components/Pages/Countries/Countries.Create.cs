using Microsoft.AspNetCore.Components;
using MudBlazor;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Client.UI.Components.Pages.Countries;

public partial class Countries
{
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    private bool _creating;
    private IDialogReference? _createDialog;

    private Task OpenCreateAsync() => OpenEditorAsync(null);
    private Task OpenEditAsync() => CanEdit ? OpenEditorAsync(_selectedIds.Single()) : Task.CompletedTask;

    private async Task OpenEditorAsync(Guid? countryId)
    {
        if (_creating || _disposed || _loading || _querying || (countryId.HasValue ? !_capabilities.CanUpdate : !_capabilities.CanCreate)) return;
        _creating = true;
        try
        {
            var parameters = new DialogParameters<CountryEditorDialog>();
            parameters.Add(x => x.CountryId, countryId);
            _createDialog = await Dialogs.ShowAsync<CountryEditorDialog>(
                countryId.HasValue ? "Edit country" : "Add country", parameters, new DialogOptions
                {
                    Position = DialogPosition.CenterRight,
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    CloseButton = false,
                    BackdropClick = false,
                    CloseOnEscapeKey = false,
                    CloseOnNavigation = false
                });
            if (_disposed) { _createDialog.Close(); return; }
            var result = await _createDialog.Result;
            if (_disposed || result is null || result.Canceled) return;
            if (result.Data is CountryDetailsDto)
            {
                _search = string.Empty;
                Snackbar.Add(countryId.HasValue ? "Country updated." : "Country created.", Severity.Success);
            }
            if (result.Data is CountryDetailsDto saved) await LocateSavedAsync(saved.Id);
            else await LoadAsync();
        }
        finally
        {
            _createDialog = null;
            _creating = false;
        }
    }
}
