using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Details;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class RecordDetailsTests
{
    [Fact]
    public async Task Details_close_and_cancel_pending_load_on_logout()
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, Guid.NewGuid());
        await using var services = TableComponentTests.CreateServices(s => s.AddSingleton<IClientAuthState>(state));
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Host>(new()));
        await renderer.Dispatcher.InvokeAsync(host.OpenAsync);
        await renderer.Dispatcher.InvokeAsync(() => state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, null, ClientSessionInvalidationReason.SignedOut));
        Assert.True(host.Token.IsCancellationRequested);
        Assert.True((await host.Reference!.Result!)!.Canceled);
        host.Pending.SetResult("late response");
        await renderer.Dispatcher.InvokeAsync(() => Task.CompletedTask);
        Assert.Equal(0, host.RenderedItems);
        Assert.Empty(renderer.Errors);
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;
        public IDialogReference? Reference;
        public CancellationToken Token;
        public int RenderedItems;
        public TaskCompletionSource<string> Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task OpenAsync()
        {
            var args = new DialogParameters<RecordDetailsDialog<string>>();
            args.Add(x => x.Load, new Func<CancellationToken, Task<string>>(ct => { Token = ct; return Pending.Task; }));
            args.Add(x => x.ChildContent, new RenderFragment<string>(item => builder => { RenderedItems++; builder.AddContent(0, item); }));
            Reference = await Dialogs.ShowAsync<RecordDetailsDialog<string>>("Details", args);
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder) { builder.OpenComponent<MudDialogProvider>(0); builder.CloseComponent(); }
    }
}
