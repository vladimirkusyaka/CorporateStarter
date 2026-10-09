using CorporateStarter.Client.Core.Audit;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Audit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Changes;

public partial class Changes
{
    [Inject] private ChangesClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Date, Entity, Record, Action, User }
    private static readonly TableColumn<ChangeListItemDto, Column>[] Columns =
    [
        new(Column.Date, "createdAtUtc", "Date (UTC)", x => x.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss")),
        new(Column.Entity, "entityName", "Entity", x => x.EntityName),
        new(Column.Record, "entityId", "Record", x => x.EntityId),
        new(Column.Action, "action", "Action", x => x.Action),
        new(Column.User, "userEmail", "User", x => x.UserEmail)
    ];
    private Task<IDialogReference> OpenAsync(ChangeListItemDto item)
    {
        var parameters = new DialogParameters<ChangeDetailsDialog>(); parameters.Add(x => x.ChangeId, item.Id);
        return Dialogs.ShowAsync<ChangeDetailsDialog>("Change details", parameters, new DialogOptions
        { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true, CloseOnNavigation = true });
    }
}
