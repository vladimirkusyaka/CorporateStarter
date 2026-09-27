using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Countries;

public partial class Countries
{
    [Inject] private CountriesClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Code, Name, NativeName, PhoneCode, Status }
    private static readonly TableColumn<CountryListItemDto, Column>[] Columns =
    [
        new(Column.Code, "code", "Code", x => x.Code),
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.NativeName, "nativeName", "Native name", x => x.NativeName),
        new(Column.PhoneCode, "phoneCode", "Phone code", x => x.PhoneCode),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<CountryEditorDialog>();
        parameters.Add(x => x.CountryId, id);
        return Dialogs.ShowAsync<CountryEditorDialog>(id.HasValue ? "Edit country" : "Add country", parameters,
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
