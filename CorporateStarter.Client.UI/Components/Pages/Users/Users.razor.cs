using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Security.Users;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Security.Users;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Users;

public partial class Users
{
    [Inject] private UsersClient Client { get; set; } = default!;
    [Inject] private IClientAuthState Auth { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Login, DisplayName, Email, Status }
    private static readonly TableColumn<UserTableItemDto, Column>[] Columns =
    [
        new(Column.Login, "login", "Login", x => x.Login),
        new(Column.DisplayName, "displayName", "Display name", x => x.DisplayName),
        new(Column.Email, "email", "Email", x => x.Email),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private IReadOnlyList<TableRecordAction<UserTableItemDto>> _actions = [];
    protected override void OnInitialized() => _actions =
    [new("Change password", Icons.Material.Filled.Key, _ => Client.CanChangePassword, OpenPasswordAsync)];
    // The dialog checks its own capability on every opening; API enforces it on every write.
    private async Task<IDialogReference> OpenPasswordAsync(UserTableItemDto user)
    {
        var parameters = new DialogParameters<UserPasswordDialog>();
        parameters.Add(x => x.UserId, user.Id);
        return await Dialogs.ShowAsync<UserPasswordDialog>("Change password", parameters, Options);
    }
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<UserEditorDialog>();
        parameters.Add(x => x.UserId, id);
        return Dialogs.ShowAsync<UserEditorDialog>(id.HasValue ? "Edit user" : "Add user", parameters, Options);
    }
    private static readonly DialogOptions Options = new()
    {
        Position = DialogPosition.CenterRight, MaxWidth = MaxWidth.Small, FullWidth = true,
        CloseButton = false, BackdropClick = false, CloseOnEscapeKey = false, CloseOnNavigation = false
    };
}
