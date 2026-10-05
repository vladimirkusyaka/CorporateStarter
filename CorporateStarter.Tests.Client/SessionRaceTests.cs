using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class SessionRaceTests
{
    [Fact]
    public async Task Logout_during_refresh_invalidates_reply_and_runs_once_after_refresh()
    {
        var (state, transport, session) = Create();
        var refresh = session.RestoreAsync();
        await transport.Started.Task;
        var logout = session.LogoutAsync();
        Assert.Same(logout, session.LogoutAsync());
        Assert.True(session.IsLogoutPending);
        Assert.Null(state.Current.UserId);
        Assert.Equal(0, transport.Logouts);
        transport.Reply.SetResult(ClientSessionRestoreResult.Authenticated(transport.User));
        Assert.False(await refresh);
        Assert.Equal(ClientLogoutStatus.SignedOut, await logout);
        Assert.Equal(1, transport.Logouts);
        Assert.Equal(ClientAuthStatus.Anonymous, state.Current.Status);
        Assert.False(session.IsLogoutPending);
    }

    [Fact]
    public async Task Reconnect_queued_before_logout_cannot_restore_after_it()
    {
        var (state, transport, session) = Create();
        var refresh = session.RestoreAsync();
        await transport.Started.Task;
        session.SuspendForReconnect();
        var reconnect = session.RestoreAfterReconnectAsync();
        var logout = session.LogoutAsync();
        transport.Reply.SetResult(ClientSessionRestoreResult.Authenticated(transport.User));
        await refresh;
        Assert.False(await reconnect);
        await logout;
        Assert.Equal(1, transport.Restores);
        Assert.Equal(ClientAuthStatus.Anonymous, state.Current.Status);
    }

    [Fact]
    public async Task Reconnect_supersedes_old_refresh_and_verifies_again()
    {
        var (state, transport, session) = Create();
        var refresh = session.RestoreAsync();
        await transport.Started.Task;
        session.SuspendForReconnect();
        var reconnect = session.RestoreAfterReconnectAsync();
        transport.Reply.SetResult(ClientSessionRestoreResult.Authenticated(transport.User));
        Assert.False(await refresh);
        Assert.True(await reconnect);
        Assert.Equal(2, transport.Restores);
        Assert.Equal(ClientAuthStatus.Authenticated, state.Current.Status);
    }

    [Fact]
    public async Task Failed_logout_blocks_refresh_and_reconnect_until_explicit_retry()
    {
        var (state, transport, session) = Create();
        transport.LogoutResult = ClientLogoutStatus.Unavailable;
        await session.LogoutAsync();
        Assert.True(session.IsLogoutPending);
        Assert.Equal(ClientAuthStatus.Unavailable, state.Current.Status);
        Assert.False(await session.RestoreAsync());
        session.SuspendForReconnect();
        Assert.False(await session.RestoreAfterReconnectAsync());
        await session.HandleSessionChangedAsync();
        Assert.Equal(0, transport.Restores);
        transport.LogoutResult = ClientLogoutStatus.SignedOut;
        await session.LogoutAsync();
        Assert.Equal(2, transport.Logouts);
        Assert.False(session.IsLogoutPending);
        Assert.Equal(ClientAuthStatus.Anonymous, state.Current.Status);
    }

    [Fact]
    public async Task Reconnect_and_external_notification_during_logout_do_not_resurrect_session()
    {
        var (state, transport, session) = Create();
        transport.LogoutReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var logout = session.LogoutAsync();
        session.SuspendForReconnect();
        Assert.False(await session.RestoreAfterReconnectAsync());
        var notification = session.HandleSessionChangedAsync();
        transport.LogoutReply.SetResult(ClientLogoutStatus.SignedOut);
        await Task.WhenAll(logout, notification);
        Assert.Equal(0, transport.Restores);
        Assert.Equal(ClientAuthStatus.Anonymous, state.Current.Status);
    }

    [Fact]
    public async Task New_circuit_recovers_pending_browser_logout_and_allows_retry()
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        var transport = new Transport();
        transport.Reply.SetResult(ClientSessionRestoreResult.LogoutPending);
        var session = new ClientSessionCoordinator(state, transport, transport, transport);
        await session.RestoreAsync();
        Assert.True(session.IsLogoutPending);
        Assert.Equal(ClientAuthStatus.Unavailable, state.Current.Status);
        Assert.Null(state.Current.UserId);
        await session.LogoutAsync();
        Assert.Equal(1, transport.Logouts);
        Assert.Equal(ClientAuthStatus.Anonymous, state.Current.Status);
    }

    private static (ClientAuthStateStore, Transport, ClientSessionCoordinator) Create()
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        var transport = new Transport();
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.User);
        return (state, transport, new(state, transport, transport, transport));
    }

    private sealed class Transport : IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid User { get; } = Guid.NewGuid();
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<ClientSessionRestoreResult> Reply { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<ClientLogoutStatus>? LogoutReply { get; set; }
        public ClientLogoutStatus LogoutResult { get; set; } = ClientLogoutStatus.SignedOut;
        public int Restores, Logouts;
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
        {
            Restores++;
            Started.TrySetResult();
            return Reply.Task;
        }
        public Task<ClientLogoutStatus> LogoutAsync()
        {
            Logouts++;
            return LogoutReply?.Task ?? Task.FromResult(LogoutResult);
        }
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
    }
}
