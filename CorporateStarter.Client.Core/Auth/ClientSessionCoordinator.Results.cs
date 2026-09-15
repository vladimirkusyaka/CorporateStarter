using CorporateStarter.Client.Abstractions.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
    public sealed partial class ClientSessionCoordinator
    {
        private bool ApplyResult(
            long revision,
            Guid? previousUserId,
            ClientSessionRestoreResult result)
        {
            switch (result.Status)
            {
                case ClientSessionRestoreStatus.Authenticated:
                    var reason = previousUserId is not null
                        && previousUserId != result.UserId
                            ? ClientSessionInvalidationReason.IdentityChanged
                            : (ClientSessionInvalidationReason?)null;

                    return _state.TryTransition(
                        revision, ClientAuthStatus.Authenticated,
                        result.UserId, reason);

                case ClientSessionRestoreStatus.Anonymous:
                    return _state.TryTransition(
                        revision, ClientAuthStatus.Anonymous,
                        invalidationReason: previousUserId is not null
                            ? ClientSessionInvalidationReason.SessionRejected
                            : null);

                case ClientSessionRestoreStatus.Unavailable:
                    return _state.TryTransition(
                        revision, ClientAuthStatus.Unavailable, previousUserId);

                case ClientSessionRestoreStatus.UnsupportedEnvironment:
                    // Losing a capability is not proof that an existing session ended.
                    return previousUserId is not null
                        ? _state.TryTransition(
                            revision, ClientAuthStatus.Unavailable, previousUserId)
                        : _state.TryTransition(
                            revision, ClientAuthStatus.UnsupportedEnvironment);

                default:
                    throw new InvalidOperationException(
                        "Unknown session restoration result.");
            }
        }
    }
}
