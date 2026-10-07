using System.Reflection;
using CorporateStarter.Client.UI.Components.Lookups;
using Microsoft.AspNetCore.Components;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class TreeGroupSelectionTests
{
    [Theory]
    [InlineData(0, "false")]
    [InlineData(1, "mixed")]
    [InlineData(2, "true")]
    public async Task Group_state_and_toggle_follow_empty_partial_full_cycle(int count, string state)
    {
        var a = new SelectionOption(Guid.NewGuid(), "Read", "Cities");
        var b = new SelectionOption(Guid.NewGuid(), "Write", "Cities");
        var other = Guid.NewGuid();
        var selected = new HashSet<Guid> { other };
        if (count > 0) selected.Add(a.Id);
        if (count > 1) selected.Add(b.Id);
        IReadOnlySet<Guid>? changed = null;
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var tree = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<ReferenceTreeSelect>(new()
        {
            ["Options"] = new[] { a, b },
            ["Selected"] = selected,
            ["SelectedChanged"] = EventCallback.Factory.Create<IReadOnlySet<Guid>>(this, ids => changed = ids)
        }));
        Assert.Equal(state, Call(tree, "GroupAria", "Cities"));
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            // A search showing only one child must not narrow the branch operation.
            typeof(ReferenceTreeSelect).GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tree, "Read");
            await (Task)Call(tree, "ToggleGroupAsync", "Cities")!;
            await tree.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Selected"] = changed }));
        });
        Assert.Contains(other, changed!);
        Assert.Equal(count == 2 ? "false" : "true", Call(tree, "GroupAria", "Cities"));
        if (count == 1)
        {
            Assert.Contains(a.Id, changed!); Assert.Contains(b.Id, changed!);
            await renderer.Dispatcher.InvokeAsync(() => (Task)Call(tree, "ToggleGroupAsync", "Cities")!);
            Assert.True(changed!.SetEquals(new[] { other }));
        }
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task Group_toggle_preserves_locked_children_and_respects_read_only()
    {
        var active = new SelectionOption(Guid.NewGuid(), "Read", "Cities");
        var locked = new SelectionOption(Guid.NewGuid(), "Old", "Cities", false);
        IReadOnlySet<Guid>? changed = null;
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var tree = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<ReferenceTreeSelect>(new()
        {
            ["Options"] = new[] { active, locked },
            ["Selected"] = new HashSet<Guid> { active.Id, locked.Id },
            ["SelectedChanged"] = EventCallback.Factory.Create<IReadOnlySet<Guid>>(this, ids => changed = ids)
        }));
        await renderer.Dispatcher.InvokeAsync(() => (Task)Call(tree, "ToggleGroupAsync", "Cities")!);
        Assert.True(changed!.SetEquals(new[] { locked.Id }));
        changed = null;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await tree.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Disabled"] = true }));
            await (Task)Call(tree, "ToggleGroupAsync", "Cities")!;
        });
        Assert.Null(changed);
        Assert.Empty(renderer.Errors);
    }
    private static object? Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
}
