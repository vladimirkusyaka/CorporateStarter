using System.Reflection;
using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.Security.Users;
using CorporateStarter.Client.UI.Components.Pages.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class UserEditorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tabs_keep_fields_and_assigned_roles_and_save_once(bool editing)
    {
        var transport = new Transport();
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.User);
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        await using var services = TableComponentTests.CreateServices(s =>
        {
            s.AddSingleton<IClientAuthState>(state);
            s.AddSingleton(new UsersClient(api));
        });
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Host>(new()));
        await renderer.Dispatcher.InvokeAsync(() => host.OpenAsync(editing ? transport.Record : null));
        var editor = host.Editor!;
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        Assert.Equal(1, editor.Tab);
        if (!editing)
        {
            await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
            Assert.Equal(0, editor.Tab);
            Assert.Equal(0, transport.Writes);
            await renderer.Dispatcher.InvokeAsync(() => editor.Fill());
            await renderer.Dispatcher.InvokeAsync(() => editor.Select(transport.ActiveRole));
        }
        else
        {
            Assert.Contains(transport.InactiveRole, editor.Selected);
            Assert.Contains(transport.ActiveRole, editor.Selected);
        }
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(0));
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        Assert.Equal("tester", editor.Login);
        await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
        Assert.Equal(1, transport.Writes);
        using var body = JsonDocument.Parse(transport.Body!);
        if (editing)
        {
            Assert.False(body.RootElement.TryGetProperty("roleIds", out _));
            Assert.False(body.RootElement.TryGetProperty("password", out _));
            Assert.Equal(transport.Person, body.RootElement.GetProperty("personId").GetGuid());
        }
        else Assert.Equal(transport.ActiveRole, body.RootElement.GetProperty("roleIds")[0].GetGuid());
        Assert.Empty(renderer.Errors);
    }
    public sealed class ProbeEditor : UserEditorDialog
    {
        [Parameter] public Action<ProbeEditor>? Capture { get; set; }
        protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Capture?.Invoke(this); }
        public int Tab => (int)typeof(UserEditorDialog).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        public IReadOnlySet<Guid> Selected => (IReadOnlySet<Guid>)typeof(UserEditorDialog).GetField("_selected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        public string Login => _model.Login;
        private object? Call(string name, params object[] args) => typeof(UserEditorDialog).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, args);
        public void SwitchTab(int index) { Call("ChangeTab", index); StateHasChanged(); }
        public Task SaveEditorAsync() => (Task)Call("SaveTabbedAsync")!;
        public void Select(Guid id) { Call("RolesChanged", new HashSet<Guid> { id }); StateHasChanged(); }
        public void Fill()
        {
            _model.Login = "tester"; _model.Email = "tester@example.test"; _model.Password = "Sample!2468Password";
            typeof(UserEditorDialog).GetField("_confirmation", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, _model.Password);
            StateHasChanged();
        }
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public ProbeEditor? Editor { get; private set; }
        public async Task OpenAsync(Guid? id)
        {
            var parameters = new DialogParameters<ProbeEditor>();
            parameters.Add(x => x.UserId, id);
            parameters.Add(x => x.Capture, new Action<ProbeEditor>(e => Editor = e));
            await Dialogs.ShowAsync<ProbeEditor>("User", parameters);
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        { builder.OpenComponent<MudDialogProvider>(0); builder.CloseComponent(); }
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid User { get; } = Guid.NewGuid();
        public Guid Record { get; } = Guid.NewGuid();
        public Guid Person { get; } = Guid.NewGuid();
        public Guid ActiveRole { get; } = Guid.NewGuid();
        public Guid InactiveRole { get; } = Guid.NewGuid();
        public int Writes;
        public string? Body;
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string path, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            string json;
            if (method != HttpMethod.Get) { Writes++; Body = jsonBody; json = JsonSerializer.Serialize(new { succeeded = true, value = Record }); }
            else if (path.EndsWith("capabilities")) json = "{\"table\":{\"canCreate\":true,\"canUpdate\":true,\"canDelete\":true,\"viewInactive\":true,\"canRestore\":true},\"canChangePassword\":false}";
            else if (path.EndsWith("role-options")) json = JsonSerializer.Serialize(new[] { new { id = ActiveRole, name = "Reader", isActive = true }, new { id = InactiveRole, name = "Retired", isActive = false } });
            else json = JsonSerializer.Serialize(new { id = Record, login = "tester", email = "tester@example.test", isActive = true, personId = Person,
                roles = new[] { new { roleId = ActiveRole, name = "Reader" }, new { roleId = InactiveRole, name = "Retired" } } });
            return Task.FromResult(new ClientApiResponse(200, json, "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(User));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
