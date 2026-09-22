using CorporateStarter.Client.Abstractions.Auth;

namespace CorporateStarter.Client.Core.Auth;

public sealed partial class ClientSessionCoordinator
{
    public Task<ClientAuthSnapshot> RestoreForApiAsync(
        ClientAuthSnapshot requestSession,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestSession);
        if (requestSession.UserId is null || requestSession.Status is not
            (ClientAuthStatus.Authenticated or ClientAuthStatus.Revalidating))
            throw new ArgumentException(
                "An authenticated or revalidating user snapshot is required.", nameof(requestSession));

        cancellationToken.ThrowIfCancellationRequested();

        // Cancelling one reader must not cancel a rotation needed by other readers.
        return RestoreForApiCoreAsync(requestSession).WaitAsync(cancellationToken);
    }

    private async Task<ClientAuthSnapshot> RestoreForApiCoreAsync(
        ClientAuthSnapshot requestSession)
    {
        await _operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var current = _state.Current;

            // A completed refresh, reconnect or identity change supersedes this request.
            if (_logoutPending ||
                current.Status != ClientAuthStatus.Authenticated ||
                current.UserId != requestSession.UserId ||
                current.SessionGeneration != requestSession.SessionGeneration ||
                current.Revision != requestSession.Revision)
            {
                return current;
            }

            if (!_state.TryTransition(
                current.Revision, ClientAuthStatus.Revalidating, current.UserId))
            {
                return _state.Current;
            }

            await RestoreCoreAsync(
                checked(current.Revision + 1), current.UserId, CancellationToken.None)
                .ConfigureAwait(false);

            return _state.Current;
        }
        finally
        {
            _operationGate.Release();
        }
    }
}
