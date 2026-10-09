using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.Directory.Persons;
using CorporateStarter.Client.UI.Components.Lookups;
using CorporateStarter.Client.UI.Components.Pages.Persons;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using System.Reflection;
using System.Text.Json;
using Xunit;
using static CorporateStarter.Tests.Client.EditorLifecycleTests;
namespace CorporateStarter.Tests.Client;

public sealed class PersonEditorTests
{
    [Fact]
    public void Shared_pages_have_unique_routes_and_persons_has_its_own_route()
    {
        var routes = typeof(PersonEditorDialog).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => route.Template)).ToArray();
        Assert.Contains("/persons", routes);
        Assert.DoesNotContain(routes.GroupBy(x => x, StringComparer.OrdinalIgnoreCase), group => group.Count() > 1);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tabs_preserve_fields_and_company_can_be_cleared_before_one_save(bool editing)
    {
        var transport = new Transport();
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.User);
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        await using var services = TableComponentTests.CreateServices(s => { s.AddSingleton<IClientAuthState>(state); s.AddSingleton(new PersonsClient(api)); });
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Host>(new()));
        await renderer.Dispatcher.InvokeAsync(() => host.OpenAsync(editing ? transport.Record : null));
        var editor = host.Editor!;
        if (!editing)
        {
            await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(2));
            await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
            Assert.Equal(0, editor.Tab); Assert.Equal(0, transport.Writes);
        }
        await renderer.Dispatcher.InvokeAsync(editor.Fill);
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(1));
        await renderer.Dispatcher.InvokeAsync(() => editor.SwitchTab(2));
        await renderer.Dispatcher.InvokeAsync(() => editor.SelectCompany(new LookupOption(Guid.NewGuid(), "Company")));
        await renderer.Dispatcher.InvokeAsync(() => editor.SelectCompany(null));
        await renderer.Dispatcher.InvokeAsync(() => editor.SelectPosition(null));
        Assert.True(editor.Dirty);
        await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
        Assert.Equal(1, transport.Writes);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal("Example", body.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("office@example.test", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Main", body.RootElement.GetProperty("description").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("companyId").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("positionId").ValueKind);
        Assert.Equal("1990-02-03", body.RootElement.GetProperty("dateOfBirth").GetString());
        if (editing) Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        else Assert.False(body.RootElement.TryGetProperty("isActive", out _));
        Assert.Empty(renderer.Errors);
    }
    public sealed class Probe : PersonEditorDialog
    {
        [Parameter] public Action<Probe>? Capture { get; set; }
        protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Capture?.Invoke(this); }
        public bool Dirty => IsDirty;
        public int Tab => (int)typeof(PersonEditorDialog).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        private object? Call(string name, params object?[] args) => typeof(PersonEditorDialog).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, args);
        public void SwitchTab(int index) { Call("ChangeTab", index); StateHasChanged(); }
        public Task SaveEditorAsync() => (Task)Call("SaveTabbedAsync")!;
        public void SelectCompany(LookupOption? option) { Call("CompanyChanged", new object?[] { option }); StateHasChanged(); }
        public void SelectPosition(LookupOption? option) { Call("PositionChanged", new object?[] { option }); StateHasChanged(); }
        public void Fill() { typeof(PersonEditorDialog).GetProperty("BirthDate", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, new DateTime(1990, 2, 3)); _model.FirstName = "Example"; _model.Email = "office@example.test"; _model.Description = "Main"; _model.IsActive = false; StateHasChanged(); }
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public Probe? Editor;
        public async Task OpenAsync(Guid? id)
        {
            var args = new DialogParameters<Probe>(); args.Add(x => x.PersonId, id); args.Add(x => x.Capture, new Action<Probe>(x => Editor = x));
            await Dialogs.ShowAsync<Probe>("Person", args);
        }
        protected override void BuildRenderTree(RenderTreeBuilder b) { b.OpenComponent<MudDialogProvider>(0); b.CloseComponent(); }
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid User { get; } = Guid.NewGuid();
        public Guid Record { get; } = Guid.NewGuid();
        public int Writes;
        public string? Body;
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string path, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            if ((path.EndsWith("company-options") || path.EndsWith("position-options"))) return Task.FromResult(new ClientApiResponse(200, "[]", "application/json"));
            if (method != HttpMethod.Get) { Writes++; Body = jsonBody; }
            var json = path.EndsWith("capabilities") ? "{\"canCreate\":true,\"canUpdate\":true,\"canDelete\":true,\"viewInactive\":true,\"canRestore\":true}" :
                JsonSerializer.Serialize(new { id = Record, firstName = "Before", email = "office@example.test", description = "Before", companyId = Guid.NewGuid(), companyName = "Company", positionId = Guid.NewGuid(), positionName = "Position", isActive = true });
            return Task.FromResult(new ClientApiResponse(200, json, "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(User));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}

