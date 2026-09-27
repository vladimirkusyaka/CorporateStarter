using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class EditorLifecycleTests
{
    [Fact]
    public async Task Dirty_editor_requires_discard_confirmation()
    {
        await using var services = Services();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<EditorHost>(new()));
        await renderer.Dispatcher.InvokeAsync(host.OpenAsync);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            host.Editor!.ChangeName("Changed");
            await host.Editor.CloseEditor();
            Assert.True(host.Editor.Confirming);
            Assert.False(host.Reference!.Result!.IsCompleted);
            host.Editor.DiscardChanges();
        });
        Assert.True((await host.Reference!.Result!)!.Canceled);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task Uncertain_save_cannot_be_repeated_and_close_requests_list_refresh()
    {
        await using var services = Services();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<EditorHost>(new()));
        await renderer.Dispatcher.InvokeAsync(host.OpenAsync);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await host.Editor!.SaveEditor();
            Assert.True(host.Editor.Uncertain);
            await host.Editor.SaveEditor();
            Assert.Equal(1, host.Editor.Writes);
            await host.Editor.CloseEditor();
        });
        var result = await host.Reference!.Result!;
        Assert.NotNull(result);
        Assert.False(result.Canceled);
        Assert.Null(result.Data);
        Assert.Empty(renderer.Errors);
    }

    private static ServiceProvider Services() => TableComponentTests.CreateServices(services =>
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, Guid.NewGuid());
        services.AddSingleton<IClientAuthState>(state);
    });

    public class Model { public string Name { get; set; } = "Initial"; }
    public sealed class ProbeEditor : RecordEditorBase<Model, Guid>
    {
        [Parameter] public Action<ProbeEditor>? Capture { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Capture?.Invoke(this);
        }
        private string _baseline = "";
        public int Writes { get; private set; }
        public bool Confirming => _confirmDiscard;
        public bool Uncertain => _uncertain;
        protected override Guid? RecordId => null;
        protected override bool IsDirty => _model.Name != _baseline;
        protected override string ConflictMessage => "Conflict";
        protected override void CaptureBaseline() => _baseline = _model.Name;
        protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Task.FromResult(new TableCapabilities(true, true, true, true, true));
        protected override Task<Model> ReadModelAsync(Guid id, CancellationToken ct) => Task.FromResult(new Model());
        protected override Task<Guid> WriteModelAsync(CancellationToken ct)
        {
            Writes++;
            throw new ClientApiTransportException("Lost response");
        }
        public void ChangeName(string name) => _model.Name = name;
        public Task CloseEditor() => CloseAsync();
        public Task SaveEditor() => SaveAsync();
        public void DiscardChanges() => Discard();
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditorDialogFrame>(0);
            builder.AddComponentReferenceCapture(1, component => _frame = (EditorDialogFrame)component);
            builder.CloseComponent();
        }
    }
    public sealed class EditorHost : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public ProbeEditor? Editor { get; private set; }
        public IDialogReference? Reference { get; private set; }
        public async Task OpenAsync()
        {
            var parameters = new DialogParameters<ProbeEditor>();
            parameters.Add(x => x.Capture, new Action<ProbeEditor>(editor => Editor = editor));
            Reference = await Dialogs.ShowAsync<ProbeEditor>("Test editor", parameters);
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
        }
    }
}
