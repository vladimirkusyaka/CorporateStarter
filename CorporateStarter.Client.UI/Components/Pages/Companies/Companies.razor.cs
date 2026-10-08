using CorporateStarter.Client.Core.Directory.Companies;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Companies;

public partial class Companies
{
    [Inject] private CompaniesClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Name, Code, City, Country, Email, Phone, Status }
    private static readonly TableColumn<CompanyListItemDto, Column>[] Columns =
    [
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.Code, "code", "Code", x => x.Code),
        new(Column.City, "cityName", "City", x => x.CityName),
        new(Column.Country, "countryName", "Country", x => x.CountryName),
        new(Column.Email, "email", "Email", x => x.Email),
        new(Column.Phone, "phone", "Phone", x => x.Phone),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<CompanyEditorDialog>();
        parameters.Add(x => x.CompanyId, id);
        return Dialogs.ShowAsync<CompanyEditorDialog>(id.HasValue ? "Edit company" : "Add company", parameters,
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
