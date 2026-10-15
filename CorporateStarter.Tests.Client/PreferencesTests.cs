using System.Reflection;
using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.Configuration;
using CorporateStarter.Client.UI.Components.Pages.Preferences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class PreferencesTests
{
    [Fact]
    public async Task Read_only_page_does_not_request_connections_or_submit_changes()
    {
        var transport = new Transport(); var (client, _, _) = Create(transport);
        await using var services = TableComponentTests.CreateServices(s => s.AddSingleton(client));
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var page = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Preferences>(new()));
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await (Task)typeof(Preferences).GetMethod("ChangeTabAsync", flags)!.Invoke(page, [2])!;
            await (Task)typeof(Preferences).GetMethod("SaveAsync", flags)!.Invoke(page, null)!;
        });
        Assert.Equal(1, transport.Calls); Assert.Empty(renderer.Errors);
    }
    [Fact]
    public async Task Save_updates_branding_and_lost_write_is_not_replayed()
    {
        var transport = new Transport(); var (client, _, branding) = Create(transport);
        var result = await client.UpdateAsync(new() { Name = "New name", Revision = Guid.NewGuid() });
        Assert.Equal("New name", branding.Name); Assert.Equal("/api/preferences/general", transport.Path);
        transport.Fail = true;
        await Assert.ThrowsAsync<ClientApiTransportException>(() => client.UpdateAsync(new() { Name = "Other", Revision = result.Revision }));
        Assert.Equal(2, transport.Calls); Assert.Equal("New name", branding.Name);
    }
    [Fact]
    public async Task Late_response_after_logout_does_not_change_branding()
    {
        var transport = new Transport { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var (client, state, branding) = Create(transport);
        var pending = client.GetAsync();
        state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, null, ClientSessionInvalidationReason.SignedOut);
        transport.Pending.SetResult(new(200, "{\"name\":\"Late\"}", "application/json"));
        await Assert.ThrowsAnyAsync<Exception>(() => pending);
        Assert.Equal("CorporateStarter", branding.Name);
    }
    private static (PreferencesClient, ClientAuthStateStore, ApplicationBranding) Create(Transport t)
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, t.UserId);
        var api = new ClientApiClient(t, state, new ClientSessionCoordinator(state, t, t, t));
        var branding = new ApplicationBranding();
        return (new PreferencesClient(api, state, branding), state, branding);
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid UserId = Guid.NewGuid(); public int Calls; public string? Path; public bool Fail;
        public TaskCompletionSource<ClientApiResponse>? Pending;
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            Calls++; Path = relativePath;
            if (Fail) throw new ClientApiTransportException("Lost response.");
            return Pending?.Task ?? Task.FromResult(new ClientApiResponse(200,
                JsonSerializer.Serialize(new { name = method == HttpMethod.Get ? "CorporateStarter" : "New name", revision = Guid.NewGuid() }), "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(UserId));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
