using CorporateStarter.Shared.Dtos.MasterData.Countries;
using MudBlazor;

namespace CorporateStarter.Web.Components.Pages.Countries;

public partial class Countries
{
    // The trash icon from the design system.
    private const string DeleteIcon = "<g fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M3 6h18'/><path d='M8 6V4a1 1 0 011-1h6a1 1 0 011 1v2'/><path d='M6 6l1 14a1 1 0 001 1h8a1 1 0 001-1l1-14'/></g>";
    private bool CanDelete => _capabilities.CanDelete && !_querying && !_loading && !_creating && !_disposed && _selectedIds.Count > 0;

    private async Task OpenDeleteAsync()
    {
        if (!CanDelete) return;
        var selected = _countries.Where(x => _selectedIds.Contains(x.Id)).ToArray();
        if (selected.Length == 0) return;
        _creating = true; // Shared dialog guard for Add, Edit and Delete.
        try
        {
            var parameters = new DialogParameters<CountryDeleteDialog>();
            parameters.Add(x => x.Countries, selected);
            _createDialog = await Dialogs.ShowAsync<CountryDeleteDialog>("Delete countries", parameters,
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
                Snackbar.Add(count == 1 ? "Country deleted (set to inactive)." :
                    $"{count} countries deleted (set to inactive).", Severity.Success);
            await LoadAsync();
        }
        finally
        {
            _createDialog = null;
            _creating = false;
        }
    }
}
