using CorporateStarter.Client.Abstractions.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
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
            CancellationToken cancellationToken = default)
        {
            if (!await _operationGate.WaitAsync(0, cancellationToken)
                    .ConfigureAwait(false))
                return false;

            try
            {
                if (_logoutPending)
                    return false;

                cancellationToken.ThrowIfCancellationRequested();
                var previous = _state.Current;
                var revision = previous.Revision;

                if (previous.Status != ClientAuthStatus.Initializing)
                {
                    var pendingStatus = previous.Status ==
                        ClientAuthStatus.UnsupportedEnvironment
                            ? ClientAuthStatus.Initializing
                            : ClientAuthStatus.Revalidating;

                    // Compute our revision, never adopt another writer's Current.
                    revision = checked(previous.Revision + 1);
                    if (!_state.TryTransition(
                            previous.Revision, pendingStatus, previous.UserId))
                        return false;
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // A synchronous state subscriber may have invalidated the session.
                    if (_state.Current.Revision != revision)
                        return false;

                    var result = await _transport.RestoreAsync(cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                    ArgumentNullException.ThrowIfNull(result);

                    return ApplyResult(revision, previous.UserId, result);
                }
                catch
                {
                    // Do not leave the UI stuck in Initializing/Revalidating.
                    // An obsolete operation cannot overwrite a newer state.
                    _state.TryTransition(
                        revision, ClientAuthStatus.Unavailable, previous.UserId);
                    throw;
                }
            }
            finally
            {
                _operationGate.Release();
            }
        }
    }
}
