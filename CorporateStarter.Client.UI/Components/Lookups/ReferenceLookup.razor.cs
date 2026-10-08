using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Lookups;

public partial class ReferenceLookup
{
    [Parameter] public string Label { get; set; } = "Record";
    [Parameter] public LookupOption? Value { get; set; }
    [Parameter] public EventCallback<LookupOption?> ValueChanged { get; set; }
    [Parameter] public bool Required { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter, EditorRequired] public Func<string, CancellationToken, Task<LookupOption[]>> Search { get; set; } = default!;
    private string? _searchError;
    private long _searchRevision;
    private string? Validate(LookupOption? value) => (Required && value is null) || value?.Id == Guid.Empty ? "Select a record from the list." : null;
    private async Task<IEnumerable<LookupOption>> SearchAsync(string text, CancellationToken ct)
    {
        var revision = ++_searchRevision;
        _searchError = null;
        if (text?.Length > 200) { _searchError = "Search must contain at most 200 characters."; return []; }
        try { return await Search(text ?? string.Empty, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return []; }
        catch (Exception)
        {
            if (revision == _searchRevision && !ct.IsCancellationRequested)
            {
                _searchError = "Could not load options. Reopen the list or change the search to retry.";
                await InvokeAsync(StateHasChanged);
            }
            return [];
        }
    }
}
