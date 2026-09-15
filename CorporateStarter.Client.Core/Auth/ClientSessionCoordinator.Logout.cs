using CorporateStarter.Client.Abstractions.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
    public sealed partial class ClientSessionCoordinator
    {
        public async Task<ClientLogoutStatus?> LogoutAsync()
        {
            if (!await _operationGate.WaitAsync(0).ConfigureAwait(false))
                return null;

            try
            {
                var previous = _state.Current;

                if (!_logoutPending &&
                    previous.Status != ClientAuthStatus.Authenticated)
                {
                    return null;
                }

                _logoutPending = true;

                if (!_state.TryTransition(
                    previous.Revision,
                    ClientAuthStatus.Anonymous,
                    invalidationReason:
                        ClientSessionInvalidationReason.SignedOut))
                {
                    return null;
                }

                var revision = checked(previous.Revision + 1);

                if (!_state.TryTransition(
                    revision,
                    ClientAuthStatus.Revalidating))
                {
                    return null;
                }

                revision = checked(revision + 1);

                try
                {
                    if (_state.Current.Revision != revision)
                        return null;

                    var result = await _logoutTransport
                        .LogoutAsync()
                        .ConfigureAwait(false);

                    if (result == ClientLogoutStatus.SignedOut)
                    {
                        _logoutPending = false;

                        return _state.TryTransition(
                            revision,
                            ClientAuthStatus.Anonymous)
                                ? result
                                : null;
                    }

                    return _state.TryTransition(
                        revision,
                        ClientAuthStatus.Unavailable)
                            ? result
                            : null;
                }
                catch
                {
                    _state.TryTransition(
                        revision,
                        ClientAuthStatus.Unavailable);

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
