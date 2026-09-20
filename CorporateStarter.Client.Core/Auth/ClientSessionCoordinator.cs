using CorporateStarter.Client.Abstractions.Auth;

namespace CorporateStarter.Client.Core.Auth;

public sealed partial class ClientSessionCoordinator
{
    private readonly ClientAuthStateStore _state;
    private readonly IClientSessionTransport _transport;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly IClientLoginTransport _loginTransport;
    private readonly IClientLogoutTransport _logoutTransport;
    private volatile bool _logoutPending;

    public bool IsLogoutPending => _logoutPending;

    public ClientSessionCoordinator(
        ClientAuthStateStore state,
        IClientSessionTransport transport,
        IClientLoginTransport loginTransport,
        IClientLogoutTransport logoutTransport)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(loginTransport);
        ArgumentNullException.ThrowIfNull(logoutTransport);

        _state = state;
        _transport = transport;
        _loginTransport = loginTransport;
        _logoutTransport = logoutTransport;
    }

    // False means busy or superseded. It does not mean anonymous.
    public async Task<bool> RestoreAsync(
    CancellationToken cancellationToken = default) =>
    await RestoreAsync(false, cancellationToken).ConfigureAwait(false);


    private async Task<bool> RestoreAsync(bool waitForCurrentOperation,
            CancellationToken cancellationToken)
    {
        if (waitForCurrentOperation)
        {
            await _operationGate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else if (!await _operationGate.WaitAsync(0, cancellationToken)
            .ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            if (_logoutPending)
                return false;

            cancellationToken.ThrowIfCancellationRequested();

            var previous = _state.Current;

            if (!waitForCurrentOperation && previous.Status == ClientAuthStatus.Revalidating)
            {
                return false;
            }

            var revision = previous.Revision;

            if (previous.Status != ClientAuthStatus.Initializing)
            {
                var pendingStatus =
                    previous.Status == ClientAuthStatus.UnsupportedEnvironment
                        ? ClientAuthStatus.Initializing
                        : ClientAuthStatus.Revalidating;

                revision = checked(previous.Revision + 1);

                if (!_state.TryTransition(
                    previous.Revision, pendingStatus, previous.UserId))
                {
                    return false;
                }
            }

            return await RestoreCoreAsync(
                revision, previous.UserId, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task HandleSessionChangedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ClientAuthSnapshot previous;

        do
        {
            previous = _state.Current;
        }
        while (!_state.TryTransition(
            previous.Revision,
            ClientAuthStatus.Revalidating,
            invalidationReason:
                ClientSessionInvalidationReason.ExternalSessionChanged));

        var revision = checked(previous.Revision + 1);

        try
        {
            await _operationGate.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            _state.TryTransition(revision, ClientAuthStatus.Unavailable);
            throw;
        }

        try
        {
            if (_state.Current.Revision != revision)
                return;

            if (_logoutPending)
            {
                _state.TryTransition(revision, ClientAuthStatus.Unavailable);
                return;
            }

            await RestoreCoreAsync(revision, null, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task<bool> RestoreCoreAsync(
        long revision,
        Guid? previousUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_state.Current.Revision != revision)
                return false;

            var result = await _transport.RestoreAsync(cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(result);

            return ApplyResult(revision, previousUserId, result);
        }
        catch
        {
            _state.TryTransition(
                revision, ClientAuthStatus.Unavailable, previousUserId);

            throw;
        }
    }
}