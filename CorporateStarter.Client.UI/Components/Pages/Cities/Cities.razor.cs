using CorporateStarter.Client.Core.MasterData.Cities;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Cities;

public partial class Cities
{
    [Inject] private CitiesClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Name, CountryCode, CountryName, Region, Status }
    private static readonly TableColumn<CityListItemDto, Column>[] Columns =
    [
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.CountryCode, "countryCode", "Country code", x => x.CountryCode),
        new(Column.CountryName, "countryName", "Country", x => x.CountryName),
        new(Column.Region, "region", "Region", x => x.Region),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<CityEditorDialog>();
        parameters.Add(x => x.CityId, id);
        return Dialogs.ShowAsync<CityEditorDialog>(id.HasValue ? "Edit city" : "Add city", parameters,
            new DialogOptions
            {
                Position = DialogPosition.CenterRight,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = false,
                BackdropClick = false,
                CloseOnEscapeKey = false,
                CloseOnNavigation = false
            });
    }
}
