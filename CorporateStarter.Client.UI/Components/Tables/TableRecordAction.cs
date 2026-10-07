using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Tables;

public sealed record TableRecordAction<TItem>(string Label, string Icon,
    Func<TItem, bool> CanExecute, Func<TItem, Task<IDialogReference>> Open);
