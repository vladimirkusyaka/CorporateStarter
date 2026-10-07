using CorporateStarter.Client.Core.Security.Roles;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Security.Roles;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Roles;

public partial class Roles
{
    [Inject] private RolesClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Name, Description, Type, Status }
    private static readonly TableColumn<RoleListItemDto, Column>[] Columns =
    [
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.Description, "description", "Description", x => x.Description),
        new(Column.Type, "type", "Type", x => x.IsSystemRole ? "System" : "Custom"),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<RoleEditorDialog>();
        parameters.Add(x => x.RoleId, id);
        return Dialogs.ShowAsync<RoleEditorDialog>(id.HasValue ? "Edit role" : "Add role", parameters,
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
