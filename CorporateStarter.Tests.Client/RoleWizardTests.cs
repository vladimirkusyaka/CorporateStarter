using System.Reflection;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.Security.Roles;
using CorporateStarter.Client.UI.Components.Lookups;
using CorporateStarter.Client.UI.Components.Pages.Roles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class RoleWizardTests
{
    [Fact]
    public async Task Tabs_allow_free_navigation_preserve_values_and_reveal_invalid_details_on_save()
    {
        var transport = new Transport();
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.User);
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        await using var services = TableComponentTests.CreateServices(s =>
        {
            s.AddSingleton<IClientAuthState>(state);
            s.AddSingleton(new RolesClient(api));
        });
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Host>(new()));
        await renderer.Dispatcher.InvokeAsync(host.OpenAsync);
        var editor = host.Editor!;
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        Assert.Equal(1, editor.Tab);
        await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
        Assert.Equal(0, editor.Tab);
        await renderer.Dispatcher.InvokeAsync(() => editor.SetFields("Auditor", "Review records", false));
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        Assert.Equal(1, editor.Tab);
        var selected = Guid.NewGuid();
        await renderer.Dispatcher.InvokeAsync(() => editor.Select(selected));
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(0));
        Assert.Equal(0, editor.Tab);
        Assert.Equal(("Auditor", "Review records", false), editor.Fields);
        Assert.Contains(selected, editor.Selected);
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        Assert.Equal(1, editor.Tab);
        await renderer.Dispatcher.InvokeAsync(() => editor.CloseEditorAsync());
        Assert.True(editor.Confirming);
        Assert.Equal(0, transport.Writes);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task Tree_changes_preserve_hidden_selections_and_ignore_disabled_options()
    {
        var hidden = new SelectionOption(Guid.NewGuid(), "cities.read", "Cities");
        var visible = new SelectionOption(Guid.NewGuid(), "companies.read", "Companies");
        var inactive = new SelectionOption(Guid.NewGuid(), "companies.old", "Companies", false);
        IReadOnlySet<Guid>? changed = null;
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var tree = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<ReferenceTreeSelect>(new()
        {
            ["Options"] = new SelectionOption[] { hidden, visible, inactive },
            ["Selected"] = new HashSet<Guid> { hidden.Id, inactive.Id },
            ["SelectedChanged"] = EventCallback.Factory.Create<IReadOnlySet<Guid>>(this, ids => changed = ids)
        }));
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            typeof(ReferenceTreeSelect).GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tree, "companies");
            await (Task)Invoke(tree, "ChangeAsync", visible, new ChangeEventArgs { Value = true })!;
        });
        Assert.NotNull(changed);
        Assert.True(changed.SetEquals(new[] { hidden.Id, visible.Id, inactive.Id }));
        changed = null;
        await renderer.Dispatcher.InvokeAsync(() => (Task)Invoke(tree, "ChangeAsync", inactive, new ChangeEventArgs { Value = false })!);
        Assert.Null(changed);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await tree.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Disabled"] = true }));
            await (Task)Invoke(tree, "ChangeAsync", visible, new ChangeEventArgs { Value = true })!;
        });
        Assert.Null(changed);
        Assert.Empty(renderer.Errors);
    }

    private static object? Invoke(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    public sealed class ProbeEditor : RoleEditorDialog
    {
        [Parameter] public Action<ProbeEditor>? Capture { get; set; }
        protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Capture?.Invoke(this); }
        public int Tab => (int)typeof(RoleEditorDialog).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        public IReadOnlySet<Guid> Selected => (IReadOnlySet<Guid>)typeof(RoleEditorDialog).GetField("_selected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        public (string, string?, bool) Fields => (_model.Name, _model.Description, _model.IsActive);
        public bool Confirming => _confirmDiscard;
        private object? Call(string name, params object[] args) => typeof(RoleEditorDialog).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, args);
        public void SwitchTab(int index) { Call("ChangeTab", index); StateHasChanged(); }
        public Task SaveEditorAsync() => (Task)Call("SaveTabbedAsync")!;
        public void Select(Guid id) { Call("PermissionsChanged", new HashSet<Guid> { id }); StateHasChanged(); }
        public void SetFields(string name, string description, bool active) { _model.Name = name; _model.Description = description; _model.IsActive = active; StateHasChanged(); }
        public Task CloseEditorAsync() => CloseAsync();
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public ProbeEditor? Editor { get; private set; }
        public async Task OpenAsync()
        {
            var parameters = new DialogParameters<ProbeEditor>();
            parameters.Add(x => x.Capture, new Action<ProbeEditor>(e => Editor = e));
            await Dialogs.ShowAsync<ProbeEditor>("Role", parameters);
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudDialogProvider>(0); builder.CloseComponent();
        }
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid User { get; } = Guid.NewGuid();
        public int Writes { get; private set; }
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            if (method != HttpMethod.Get) { Writes++; throw new InvalidOperationException("Navigation must not save."); }
            return Task.FromResult(new ClientApiResponse(200, relativePath.EndsWith("capabilities")
                ? "{\"table\":{\"canCreate\":true,\"canUpdate\":true,\"canDelete\":true,\"viewInactive\":true,\"canRestore\":true},\"canManagePermissions\":true}" : "[]", "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(User));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
