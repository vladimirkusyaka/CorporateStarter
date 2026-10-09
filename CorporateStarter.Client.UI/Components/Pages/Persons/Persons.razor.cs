using CorporateStarter.Client.Core.Directory.Persons;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Persons;

public partial class Persons
{
    [Inject] private PersonsClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { FirstName, LastName, Code, Company, Position, Email, Phone, Status }
    private static readonly TableColumn<PersonListItemDto, Column>[] Columns =
    [
        new(Column.FirstName, "firstName", "First name", x => x.FirstName),
        new(Column.LastName, "lastName", "Last name", x => x.LastName),
        new(Column.Code, "code", "Code", x => x.Code),
        new(Column.Company, "companyName", "Company", x => x.CompanyName),
        new(Column.Position, "positionName", "Position", x => x.PositionName),
        new(Column.Email, "email", "Email", x => x.Email),
        new(Column.Phone, "phone", "Phone", x => x.Phone),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<PersonEditorDialog>();
        parameters.Add(x => x.PersonId, id);
        return Dialogs.ShowAsync<PersonEditorDialog>(id.HasValue ? "Edit person" : "Add person", parameters,
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
