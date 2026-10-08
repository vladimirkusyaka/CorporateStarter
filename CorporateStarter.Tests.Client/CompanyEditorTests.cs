using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.Directory.Companies;
using CorporateStarter.Client.UI.Components.Lookups;
using CorporateStarter.Client.UI.Components.Pages.Companies;
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

public sealed class CompanyEditorTests
{
    [Fact]
    public void Shared_pages_have_unique_routes_and_companies_has_its_own_route()
    {
        var routes = typeof(CompanyEditorDialog).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => route.Template)).ToArray();
        Assert.Contains("/companies", routes);
        Assert.DoesNotContain(routes.GroupBy(x => x, StringComparer.OrdinalIgnoreCase), group => group.Count() > 1);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tabs_preserve_fields_and_city_can_be_cleared_before_one_save(bool editing)
    {
        var transport = new Transport();
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.User);
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        await using var services = TableComponentTests.CreateServices(s => { s.AddSingleton<IClientAuthState>(state); s.AddSingleton(new CompaniesClient(api)); });
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
        await renderer.Dispatcher.InvokeAsync(() => editor.SelectCity(new LookupOption(Guid.NewGuid(), "City")));
        await renderer.Dispatcher.InvokeAsync(() => editor.SelectCity(null));
        Assert.True(editor.Dirty);
        await renderer.Dispatcher.InvokeAsync(editor.SaveEditorAsync);
        Assert.Equal(1, transport.Writes);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal("Example", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("office@example.test", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Main", body.RootElement.GetProperty("street").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("cityId").ValueKind);
        if (editing) Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        else Assert.False(body.RootElement.TryGetProperty("isActive", out _));
        Assert.Empty(renderer.Errors);
    }
    [Fact]
    public async Task Optional_lookup_accepts_empty_but_required_lookup_still_rejects_it()
    {
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var lookup = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<ReferenceLookup>(new()
        { ["Search"] = new Func<string, CancellationToken, Task<LookupOption[]>>((_, _) => Task.FromResult(Array.Empty<LookupOption>())) }));
        var validate = typeof(ReferenceLookup).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(validate.Invoke(lookup, new object?[] { null }));
        await renderer.Dispatcher.InvokeAsync(() => lookup.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Required"] = false })));
        Assert.Null(validate.Invoke(lookup, new object?[] { null }));
        Assert.NotNull(validate.Invoke(lookup, new object?[] { new LookupOption(Guid.Empty, "Invalid") }));
        Assert.Empty(renderer.Errors);
    }
    public sealed class Probe : CompanyEditorDialog
    {
        [Parameter] public Action<Probe>? Capture { get; set; }
        protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Capture?.Invoke(this); }
        public bool Dirty => IsDirty;
        public int Tab => (int)typeof(CompanyEditorDialog).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
        private object? Call(string name, params object?[] args) => typeof(CompanyEditorDialog).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, args);
        public void SwitchTab(int index) { Call("ChangeTab", index); StateHasChanged(); }
        public Task SaveEditorAsync() => (Task)Call("SaveTabbedAsync")!;
        public void SelectCity(LookupOption? option) { Call("CityChanged", new object?[] { option }); StateHasChanged(); }
        public void Fill() { _model.Name = "Example"; _model.Email = "office@example.test"; _model.Street = "Main"; _model.IsActive = false; StateHasChanged(); }
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public Probe? Editor;
        public async Task OpenAsync(Guid? id)
        {
            var args = new DialogParameters<Probe>(); args.Add(x => x.CompanyId, id); args.Add(x => x.Capture, new Action<Probe>(x => Editor = x));
            await Dialogs.ShowAsync<Probe>("Company", args);
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
            if (path.EndsWith("city-options")) return Task.FromResult(new ClientApiResponse(200, "[]", "application/json"));
            if (method != HttpMethod.Get) { Writes++; Body = jsonBody; }
            var json = path.EndsWith("capabilities") ? "{\"canCreate\":true,\"canUpdate\":true,\"canDelete\":true,\"viewInactive\":true,\"canRestore\":true}" :
                JsonSerializer.Serialize(new { id = Record, name = "Example", email = "office@example.test", street = "Main", cityId = Guid.NewGuid(), cityName = "City", countryName = "Country", isActive = true });
            return Task.FromResult(new ClientApiResponse(200, json, "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(User));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}

