using CorporateStarter.Client.Core.Audit;
using CorporateStarter.Client.UI.Components.Tables;
using CorporateStarter.Shared.Dtos.Audit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.SecurityEvents;

public partial class SecurityEvents
{
    [Inject] private SecurityEventsClient Client { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    [Inject] private CorporateStarter.Client.Core.Auth.ProfilePreferences Preferences { get; set; } = default!;
    private enum Column { Date, Type, Severity, Outcome, User, UserId, Ip, Correlation, Id }
    private TableColumn<SecurityEventRowDto, Column>[] Columns =>
    [
        new(Column.Date,"createdAtUtc","Date (filter: UTC)",x => Preferences.Format(x.CreatedAtUtc)),
        new(Column.Type,"eventType","Event",x => x.EventType),
        new(Column.Severity,"severity","Severity",x => x.Severity),
        new(Column.Outcome,"outcome","Outcome",x => x.Outcome),
        new(Column.User,"subjectUserDisplayName","User",x => x.SubjectUserDisplayName ?? x.SubjectUserEmail ?? x.SubjectUserId?.ToString()),
        new(Column.UserId,"subjectUserId","User ID",x => x.SubjectUserId?.ToString()),
        new(Column.Ip,"ipAddress","IP address",x => x.IpAddress),
        new(Column.Correlation,"correlationId","Correlation ID",x => x.CorrelationId),
        new(Column.Id,"id","Event ID",x => x.Id.ToString())
    ];
    private Task<IDialogReference> OpenAsync(SecurityEventRowDto item)
    {
        var parameters = new DialogParameters<SecurityEventDetailsDialog>(); parameters.Add(x => x.EventId, item.Id);
        return Dialogs.ShowAsync<SecurityEventDetailsDialog>("Security event", parameters, new DialogOptions
        { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true, CloseOnNavigation = true });
    }
}
