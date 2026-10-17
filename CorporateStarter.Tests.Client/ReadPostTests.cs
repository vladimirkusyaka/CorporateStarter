using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.MasterData.Positions;
using CorporateStarter.Client.Core.Directory.Persons;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class ReadPostTests
{
    [Fact]
    public async Task Fifty_table_queries_and_lookups_do_not_refresh_valid_session()
    {
        var table = new Transport { Response = new(200, "{\"items\":[],\"page\":1,\"pageSize\":20,\"totalCount\":0}", "application/json") };
        var lookup = new Transport { Response = new(200, "[]", "application/json") };
        var positions = new PositionsClient(CreateApi(table));
        var persons = new PersonsClient(CreateApi(lookup));
        for (var i = 0; i < 50; i++)
        {
            await positions.QueryAsync(new() { PageSize = 20 });
            await persons.SearchCompaniesAsync("Example");
            await persons.SearchPositionsAsync("Manager");
        }
        Assert.Equal(50, table.Calls);
        Assert.Equal(100, lookup.Calls);
        Assert.Equal(0, table.Restores);
        Assert.Equal(0, lookup.Restores);
    }

    [Fact]
    public async Task Find_uses_current_session_without_refresh()
    {
        var transport = new Transport { Response = new(200, "{}", "application/json") };
        await new PositionsClient(CreateApi(transport)).FindAsync(new() { Text = "Manager" });
        Assert.Equal("/api/Positions/find", transport.Path);
        Assert.Equal(0, transport.Restores);
        Assert.Equal(1, transport.Calls);
    }

    private static Task<string[]> Query(ClientApiClient api, CancellationToken ct = default) =>
        new PositionsClient(api).FilterValuesAsync(new(), ct);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Expiry_or_401_refreshes_once_and_replays_read(bool local)
    {
        var transport = new Transport { Expired = local, FirstStatus = local ? null : 401, Response = new(200, "[]", "application/json") };
        Assert.Empty(await Query(CreateApi(transport)));
        Assert.Equal(1, transport.Restores);
        Assert.Equal(2, transport.Calls);
        Assert.Equal(HttpMethod.Post, transport.Method);
        Assert.NotNull(transport.Body);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(429)]
    public async Task Non_auth_errors_do_not_refresh_or_replay(int status)
    {
        var transport = new Transport { Response = new(status, "", "application/json") };
        await Assert.ThrowsAsync<ClientApiHttpException>(() => Query(CreateApi(transport)));
        Assert.Equal(1, transport.Calls);
        Assert.Equal(0, transport.Restores);
    }

    [Fact]
    public async Task Persistent_401_stops_after_one_retry()
    {
        var transport = new Transport { Response = new(401, "") };
        await Assert.ThrowsAsync<ClientApiHttpException>(() => Query(CreateApi(transport)));
        Assert.Equal(2, transport.Calls);
        Assert.Equal(1, transport.Restores);
    }

    [Fact]
    public async Task Lost_response_does_not_replay_read_or_write()
    {
        var read = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => Query(CreateApi(read)));
        Assert.Equal(1, read.Calls);
        Assert.Equal(0, read.Restores);
        var write = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => new PositionsClient(CreateApi(write)).CreateAsync(new() { Name = "Manager" }));
        Assert.Equal(1, write.Calls);
        Assert.Equal(1, write.Restores);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Readers_share_refresh_even_when_one_cancels(bool cancelFirst)
    {
        var transport = new Transport { Expired = true, Response = new(200, "[]", "application/json"), RestoreReply = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var api = CreateApi(transport);
        using var cts = new CancellationTokenSource();
        var first = Query(api, cts.Token);
        Assert.Equal(1, transport.Restores);
        var second = Query(api);
        if (cancelFirst)
        {
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        }
        transport.RestoreReply.SetResult(ClientSessionRestoreResult.Authenticated(transport.UserId));
        Assert.Empty(await second);
        if (!cancelFirst) Assert.Empty(await first);
        Assert.Equal(1, transport.Restores);
        Assert.Equal(cancelFirst ? 2 : 3, transport.Calls);
    }

    [Fact]
    public async Task Response_from_previous_session_is_rejected()
    {
        var transport = new Transport { Response = new(200, "[]", "application/json") };
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.UserId);
        var api = new ClientApiClient(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
        transport.OnSend = () => state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, invalidationReason: ClientSessionInvalidationReason.SignedOut);
        await Assert.ThrowsAsync<ClientApiSessionException>(() => Query(api));
        Assert.Equal(1, transport.Calls);
        Assert.Equal(0, transport.Restores);
    }

    private static ClientApiClient CreateApi(Transport transport)
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.UserId);
        return new(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
    }

    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public ClientApiResponse Response { get; init; } = new(204, "");
        public bool Fail { get; init; }
        public bool Expired { get; init; }
        public int? FirstStatus { get; init; }
        public int Restores { get; private set; }
        public TaskCompletionSource<ClientSessionRestoreResult>? RestoreReply { get; set; }
        public Action? OnSend { get; set; }
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public string? Path { get; private set; }
        public string? Body { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            Assert.Equal(UserId, expectedUserId);
            var call = Interlocked.Increment(ref _calls); Path = relativePath; Body = jsonBody; Method = method;
            OnSend?.Invoke();
            if (Expired && call == 1) throw new ClientApiSessionException(ClientApiSessionFailure.AccessTokenRequired);
            if (FirstStatus is int status && call == 1) return Task.FromResult(new ClientApiResponse(status, ""));
            if (Fail) throw new ClientApiTransportException("Lost response.");
            return Task.FromResult(Response);
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
        {
            Restores++;
            return RestoreReply?.Task ?? Task.FromResult(ClientSessionRestoreResult.Authenticated(UserId));
        }
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
