using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace CorporateStarter.Web.Components.Pages.Countries;

// null Values means no choice restriction; an empty array means no selected values.
public sealed record CountryColumnFilter(string Text = "", string[]? Values = null);

public partial class CountryFilterDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public bool IsChoice { get; set; }
    [Parameter] public string[] Options { get; set; } = [];
    [Parameter] public CountryColumnFilter Initial { get; set; } = new();
    private string _text = string.Empty;
    private HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
    protected override void OnInitialized()
    {
        _text = Initial.Text;
        _selected = new(Initial.Values ?? Options, StringComparer.OrdinalIgnoreCase);
    }
    private void SetSelected(string option, bool value)
    {
        if (value) _selected.Add(option); else _selected.Remove(option);
    }
    private void Apply() => Dialog.Close(DialogResult.Ok(IsChoice
        ? new CountryColumnFilter(Values: _selected.SetEquals(Options) ? null : _selected.ToArray())
        : new CountryColumnFilter(Text: _text.Trim())));
    private void Clear() => Dialog.Close(DialogResult.Ok(new CountryColumnFilter()));
    private void Cancel() => Dialog.Cancel();
    private void OnKeyDown(KeyboardEventArgs args) { if (args.Key == "Enter") Apply(); }
}
