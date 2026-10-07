using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.UI.Components.Lookups
{
    public partial class ReferenceTreeSelect
    {
        [Parameter] public string Label { get; set; } = "Options";
        [Parameter] public IReadOnlyList<SelectionOption> Options { get; set; } = [];
        [Parameter] public IReadOnlySet<Guid> Selected { get; set; } = new HashSet<Guid>();
        [Parameter] public EventCallback<IReadOnlySet<Guid>> SelectedChanged { get; set; }
        [Parameter] public bool Disabled { get; set; }
        private string _search = string.Empty;
        private readonly string _searchId = "tree-selection-" + Guid.NewGuid().ToString("N");
        private readonly HashSet<string> _collapsed = [];
        private IEnumerable<SelectionOption> Visible => Options.Where(x =>
            x.Label.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.Group.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Group).ThenBy(x => x.Label);
        // Group operations use the complete branch, independently of the search filter.
        private SelectionOption[] GroupOptions(string group) => Options.Where(x => x.Group == group).ToArray();
        private bool? GroupState(string group)
        {
            var all = GroupOptions(group);
            var available = all.Where(x => x.Enabled).ToArray();
            var counted = available.Length > 0 ? available : all;
            var selected = counted.Count(x => Selected.Contains(x.Id));
            return selected == 0 ? false : selected == counted.Length ? true : null;
        }
        private string GroupAria(string group) => GroupState(group) switch { true => "true", false => "false", _ => "mixed" };
        private Task ToggleGroupAsync(string group)
        {
            var available = GroupOptions(group).Where(x => x.Enabled).ToArray();
            if (Disabled || available.Length == 0) return Task.CompletedTask;
            var selectAll = GroupState(group) != true;
            var next = Selected.ToHashSet();
            foreach (var option in available)
                if (selectAll) next.Add(option.Id); else next.Remove(option.Id);
            return SelectedChanged.InvokeAsync(next);
        }
        private void SetExpanded(string group, bool expanded)
        {
            if (expanded) _collapsed.Remove(group); else _collapsed.Add(group);
        }
        private Task ChangeAsync(SelectionOption option, ChangeEventArgs args)
        {
            if (Disabled || !option.Enabled || !Options.Any(x => x.Id == option.Id && x.Enabled)) return Task.CompletedTask;
            var next = Selected.ToHashSet();
            if (args.Value is true) next.Add(option.Id); else next.Remove(option.Id);
            return SelectedChanged.InvokeAsync(next);
        }
    }
}
