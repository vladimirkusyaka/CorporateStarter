using CorporateStarter.Client.Abstractions.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
    public sealed partial class ClientSessionCoordinator
    {
        // Null means not started or superseded, not rejected credentials.
        public async Task<ClientLoginResult?> LoginAsync(
            string login,
            string password)
        {
            if (!await _operationGate.WaitAsync(0).ConfigureAwait(false))
                return null;

            try
            {
                var previous = _state.Current;

                if (_logoutPending ||
                    previous.Status != ClientAuthStatus.Anonymous)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(login) ||
                    string.IsNullOrEmpty(password))
                {
                    return new(ClientLoginStatus.InvalidInput);
                }

                var revision = checked(previous.Revision + 1);

                if (!_state.TryTransition(
                    previous.Revision,
                    ClientAuthStatus.Revalidating))
                {
                    return null;
                }

                try
                {
                    if (_state.Current.Revision != revision)
                        return null;

                    var result = await _loginTransport
                        .LoginAsync(login, password)
                        .ConfigureAwait(false);

                    ArgumentNullException.ThrowIfNull(result);

                    var status = result.Status switch
                    {
                        ClientLoginStatus.Authenticated =>
                            ClientAuthStatus.Authenticated,

                        ClientLoginStatus.Rejected or
                        ClientLoginStatus.InvalidInput or
                        ClientLoginStatus.RateLimited or
                        ClientLoginStatus.MfaRequired =>
                            ClientAuthStatus.Anonymous,

                        ClientLoginStatus.UnsupportedEnvironment =>
                            ClientAuthStatus.UnsupportedEnvironment,

                        _ => ClientAuthStatus.Unavailable
                    };

                    return _state.TryTransition(
                        revision,
                        status,
                        result.UserId)
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
