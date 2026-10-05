using CorporateStarter.Client.Core.Security.Permissions;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Permissions;
using Microsoft.AspNetCore.Components;

namespace CorporateStarter.Client.UI.Components.Pages.Permissions;

public partial class Permissions
{
    [Inject] private PermissionsClient Client { get; set; } = default!;
    private enum Column { Code, Name, Group, Description, Status }
    private static readonly TableColumn<PermissionListItemDto, Column>[] Columns =
    [
        new(Column.Code, "code", "Code", x => x.Code),
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.Group, "group", "Group", x => x.Group),
        new(Column.Description, "description", "Description", x => x.Description),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
}
