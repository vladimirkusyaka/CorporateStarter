using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Pages.Profile;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Text.Json;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace CorporateStarter.Tests.Client;

public sealed class ProfileTests
{
    [Fact]
    public void Device_time_uses_browser_zone_and_not_server_local_time()
    {
        var preferences = new ProfilePreferences();
        preferences.SetDeviceZone("America/New_York"); preferences.Apply(new() { TimeZone = "System" });
        Assert.Equal("1/2/2026 7:00:00 AM", preferences.Format(new(2026, 1, 2, 12, 0, 0, DateTimeKind.Unspecified)));
        Assert.Equal("7/2/2026 8:00:00 AM", preferences.Format(new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc)));
        preferences.Apply(new()); Assert.Equal("1/2/2026 12:00:00 PM", preferences.Format(new(2026, 1, 2, 12, 0, 0)));
    }
    [Fact]
    public async Task Profile_settings_reset_on_logout_and_ignore_late_response()
    {
        var transport = new Transport(); var state = State(transport);
        var prefs = new ProfilePreferences(); using var client = Client(transport, state, prefs);
        await client.GetAsync(); Assert.Equal("Dark", prefs.Theme); Assert.Equal(50, prefs.PageSize);
        transport.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = client.GetAsync();
        state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, null, ClientSessionInvalidationReason.SignedOut);
        Assert.Equal("System", prefs.Theme); Assert.Equal(20, prefs.PageSize);
        transport.Pending.SetResult(transport.ProfileResponse());
        await Assert.ThrowsAnyAsync<Exception>(() => pending);
        Assert.Equal("System", prefs.Theme);
    }
    [Fact]
    public async Task Writes_use_self_routes_and_do_not_replay_unknown_password_result()
    {
        var transport = new Transport(); var state = State(transport); var prefs = new ProfilePreferences();
        using var client = Client(transport, state, prefs);
        await client.SaveAccountAsync("Alice"); Assert.Equal("/api/profile/account", transport.Path);
        using var body = JsonDocument.Parse(transport.Body!); Assert.Equal("Alice", body.RootElement.GetProperty("displayName").GetString());
        Assert.False(body.RootElement.TryGetProperty("userId", out _));
        await client.SaveAppearanceAsync(new() { Theme = "Dark", PageSize = 10 }); Assert.Equal(10, prefs.PageSize);
        var calls = transport.Calls; transport.Fail = true;
        await Assert.ThrowsAsync<ClientApiTransportException>(() => client.ChangePasswordAsync(new() { CurrentPassword = "old", NewPassword = "new" }));
        Assert.Equal(calls + 1, transport.Calls); Assert.Equal("/api/profile/password", transport.Path);
    }
    [Theory]
    [InlineData("Blue", 20, "UTC")]
    [InlineData("Dark", 21, "UTC")]
    [InlineData("Light", 20, "Europe/Kyiv")]
    public void Unsupported_settings_are_rejected(string theme, int pageSize, string timeZone) =>
        Assert.False(new ProfileSettingsDto { Theme = theme, PageSize = pageSize, TimeZone = timeZone }.IsValid());
    [Fact]
    public async Task Profile_renders_and_validates_confirmation_before_sending_password()
    {
        var transport = new Transport(); var state = State(transport); var prefs = new ProfilePreferences();
        using var client = Client(transport, state, prefs);
        await using var services = TableComponentTests.CreateServices(s =>
        {
            s.AddSingleton(client); s.AddSingleton(new ClientSessionCoordinator(state, transport, transport, transport));
        });
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var page = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Profile>(new()));
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(Profile).GetField(name, flags)!.SetValue(page, value);
        object? Read(string name) => typeof(Profile).GetField(name, flags)!.GetValue(page);
        Assert.NotNull(Read("_profile")); Assert.Equal("Dark", Read("_theme"));
        var calls = transport.Calls;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Set("_currentPassword", "old"); Set("_newPassword", "new"); Set("_confirmation", "different");
            await (Task)typeof(Profile).GetMethod("ChangePasswordAsync", flags)!.Invoke(page, null)!;
        });
        Assert.Equal(calls, transport.Calls); Assert.Equal("New passwords do not match.", Read("_error"));
        Assert.Empty(renderer.Errors);
    }

    private static ClientAuthStateStore State(Transport t) { var s = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance); s.TryTransition(0, ClientAuthStatus.Authenticated, t.UserId); return s; }
    private static ProfileClient Client(Transport t, ClientAuthStateStore s, ProfilePreferences prefs)
    {
        var api = new ClientApiClient(t, s, new ClientSessionCoordinator(s, t, t, t));
        return new(api, s, prefs, new CurrentUserClient(api), new ClientPermissions(s));
    }
    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public int Calls; public bool Fail; public string? Path, Body;
        public TaskCompletionSource<ClientApiResponse>? Pending;
        public ClientApiResponse ProfileResponse() => new(200, JsonSerializer.Serialize(new { account = new { id = UserId, login = "alice" }, appearance = new { theme = "Dark", pageSize = 50, timeZone = "UTC" } }), "application/json");
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            Calls++; Path = relativePath; Body = jsonBody;
            if (Fail) throw new ClientApiTransportException("Lost response.");
            return Pending?.Task ?? Task.FromResult(method == HttpMethod.Get ? ProfileResponse() : new(200, "{\"succeeded\":true}", "application/json"));
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(ClientSessionRestoreResult.Authenticated(UserId));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => Task.FromResult(ClientLogoutStatus.SignedOut);
    }
}
