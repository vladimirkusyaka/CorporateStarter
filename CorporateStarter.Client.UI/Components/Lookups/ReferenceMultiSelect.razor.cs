using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace CorporateStarter.Client.UI.Components.Lookups
{
    public partial class ReferenceMultiSelect
    {
        [Parameter] public string Label { get; set; } = "Options";
        [Parameter] public IReadOnlyList<SelectionOption> Options { get; set; } = [];
        [Parameter] public IReadOnlySet<Guid> Selected { get; set; } = new HashSet<Guid>();
        [Parameter] public EventCallback<IReadOnlySet<Guid>> SelectedChanged { get; set; }
        [Parameter] public bool Disabled { get; set; }
        private string _search = string.Empty;
        private readonly string _searchId = "selection-" + Guid.NewGuid().ToString("N");
        private IEnumerable<SelectionOption> Visible => Options.Where(x =>
            x.Label.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.Group.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Group).ThenBy(x => x.Label);
        private Task ChangeAsync(SelectionOption option, ChangeEventArgs args)
        {
            if (Disabled || !option.Enabled || !Options.Any(x => x.Id == option.Id && x.Enabled)) return Task.CompletedTask;
            var next = Selected.ToHashSet();
            if (args.Value is true) next.Add(option.Id); else next.Remove(option.Id);
            return SelectedChanged.InvokeAsync(next);
        }
    }
}
