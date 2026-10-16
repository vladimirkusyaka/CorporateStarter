using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Dtos.Auth;
namespace CorporateStarter.Client.Core.Auth;

public sealed class ProfileClient : IDisposable
{
    private readonly ClientApiClient _api;
    private readonly IClientAuthState _auth;
    private readonly ProfilePreferences _preferences;
    private readonly CurrentUserClient _identity;
    private readonly ClientPermissions _permissions;
    private ClientAuthSnapshot _snapshot;
    private bool _disposed;
    public ProfileClient(ClientApiClient api, IClientAuthState auth, ProfilePreferences preferences, CurrentUserClient identity, ClientPermissions permissions)
    { _api = api; _auth = auth; _preferences = preferences; _identity = identity; _permissions = permissions; _snapshot = auth.Current; auth.StateChanged += Changed; }
    private void Changed(object? sender, ClientAuthStateChangedEventArgs args)
    {
        var next = _auth.Current;
        if (next.UserId != _snapshot.UserId || next.SessionGeneration != _snapshot.SessionGeneration) { _preferences.Apply(new()); _permissions.Clear(); }
        _snapshot = next;
    }
    private void Check(ClientAuthSnapshot start)
    {
        var now = _auth.Current;
        if (_disposed || now.UserId != start.UserId || now.SessionGeneration != start.SessionGeneration || now.UserId is null)
            throw new OperationCanceledException("The profile session changed.");
    }
    public async Task<MyProfileDto> GetAsync(CancellationToken ct = default)
    {
        var start = _auth.Current;
        var result = await _api.GetJsonAsync("/api/profile", ClientJsonContext.Default.MyProfileDto, ct);
        Check(start);
        if (result.Account.Id != start.UserId) throw new InvalidDataException("Unexpected profile.");
        _preferences.Apply(result.Appearance);
        _permissions.Apply(result.Account, start);
        return result;
    }
    public async Task SaveAccountAsync(string? displayName, CancellationToken ct = default)
    {
        var start = _auth.Current;
        Confirm(await _api.PutJsonAsync("/api/profile/account", new UpdateMyAccountRequest { DisplayName = displayName },
            ClientJsonContext.Default.UpdateMyAccountRequest, ClientJsonContext.Default.ProfileCommandReply, ct));
        Check(start); _identity.NotifyChanged();
    }
    public async Task SaveAppearanceAsync(ProfileSettingsDto value, CancellationToken ct = default)
    {
        var start = _auth.Current;
        Confirm(await _api.PutJsonAsync("/api/profile/appearance", value, ClientJsonContext.Default.ProfileSettingsDto, ClientJsonContext.Default.ProfileCommandReply, ct));
        Check(start); _preferences.Apply(value);
    }
    public async Task ChangePasswordAsync(ChangeMyPasswordRequest value, CancellationToken ct = default) =>
        Confirm(await _api.PostJsonAsync("/api/profile/password", value, ClientJsonContext.Default.ChangeMyPasswordRequest, ClientJsonContext.Default.ProfileCommandReply, ct));
    private static void Confirm(ProfileCommandReply reply) { if (!reply.Succeeded) throw new InvalidDataException("The operation was not confirmed."); }
    public void Dispose() { _disposed = true; _auth.StateChanged -= Changed; }
}
