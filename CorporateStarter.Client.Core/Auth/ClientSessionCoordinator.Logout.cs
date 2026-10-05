using CorporateStarter.Client.Abstractions.Auth;

namespace CorporateStarter.Client.Core.Auth;

public sealed partial class ClientSessionCoordinator
{
    private readonly object _logoutSync = new();
    private Task<ClientLogoutStatus?>? _logoutTask;

    public Task<ClientLogoutStatus?> LogoutAsync()
    {
        lock (_logoutSync)
        {
            if (_logoutTask is { IsCompleted: false }) return _logoutTask;
            if (!_logoutPending && _state.Current.UserId is null)
                return Task.FromResult<ClientLogoutStatus?>(null);

            // Record intent before waiting: in-flight refresh replies become obsolete immediately.
            _logoutPending = true;
            ClientAuthSnapshot current;
            do { current = _state.Current; }
            while (!_state.TryTransition(current.Revision, ClientAuthStatus.Anonymous,
                invalidationReason: ClientSessionInvalidationReason.SignedOut));
            PublishLogoutState(ClientAuthStatus.Revalidating);
            return _logoutTask = FinishLogoutAsync();
        }
    }

    private async Task<ClientLogoutStatus?> FinishLogoutAsync()
    {
        await _operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = await _logoutTransport.LogoutAsync().ConfigureAwait(false);
            if (result == ClientLogoutStatus.SignedOut)
            {
                _logoutPending = false;
                PublishLogoutState(ClientAuthStatus.Anonymous);
            }
            else PublishLogoutState(ClientAuthStatus.Unavailable);
            return result;
        }
        catch
        {
            PublishLogoutState(ClientAuthStatus.Unavailable);
            throw;
        }
        finally { _operationGate.Release(); }
    }

    private void PublishLogoutState(ClientAuthStatus status)
    {
        ClientAuthSnapshot current;
        do { current = _state.Current; }
        while (!_state.TryTransition(current.Revision, status));
    }
}
