using System.Reflection;
using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class UserMenuTests
{
    [Fact]
    public async Task Menu_loads_identity_and_clears_it_on_logout()
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        var user = Guid.NewGuid(); state.TryTransition(0, ClientAuthStatus.Authenticated, user);
        var transport = new Transport();
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        await using var services = TableComponentTests.CreateServices(s => { s.AddSingleton<IClientAuthState>(state); s.AddSingleton(new CurrentUserClient(api)); });
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var menu = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<UserMenu>(new()));
        string Read(string property) => (string)typeof(UserMenu).GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu)!;
        Assert.Equal("Alice", Read("Name")); Assert.Equal("A", Read("Initial"));
        Assert.Equal(1, transport.Calls);
        await renderer.Dispatcher.InvokeAsync(() => state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, null, ClientSessionInvalidationReason.SignedOut));
        Assert.Equal("Signed in", Read("Name")); Assert.Equal("?", Read("Initial"));
        Assert.Equal(1, transport.Calls);
        Assert.Empty(renderer.Errors);
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public int Calls;
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            Calls++; Assert.Equal(HttpMethod.Get, method); Assert.Equal("/api/Auth/me", relativePath);
            return Task.FromResult(new ClientApiResponse(200, JsonSerializer.Serialize(new { id = expectedUserId, login = "alice", displayName = "Alice" }), "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
