using CorporateStarter.Client.Core.MasterData.Positions;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Positions;

public partial class Positions
{
    [Inject] private PositionsClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    private enum Column { Name, Description, Status }
    private static readonly TableColumn<PositionListItemDto, Column>[] Columns =
    [
        new(Column.Name, "name", "Name", x => x.Name),
        new(Column.Description, "description", "Description", x => x.Description),
        new(Column.Status, "isActive", "Status", x => x.IsActive ? "Active" : "Inactive", true)
    ];
    private Task<IDialogReference> OpenEditorAsync(Guid? id)
    {
        var parameters = new DialogParameters<PositionEditorDialog>();
        parameters.Add(x => x.PositionId, id);
        return Dialogs.ShowAsync<PositionEditorDialog>(id.HasValue ? "Edit position" : "Add position", parameters,
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
