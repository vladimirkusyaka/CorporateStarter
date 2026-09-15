using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public sealed class ClientAuthStateChangedEventArgs : EventArgs
    {
        public ClientAuthSnapshot Snapshot { get; }

        public ClientSessionInvalidationReason? InvalidationReason { get; }

        public ClientAuthStateChangedEventArgs(
            ClientAuthSnapshot snapshot,
            ClientSessionInvalidationReason? invalidationReason = null)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (invalidationReason is { } reason && !Enum.IsDefined(reason))
                throw new ArgumentOutOfRangeException(
                    nameof(invalidationReason));

            Snapshot = snapshot;
            InvalidationReason = invalidationReason;
        }
    }
}
