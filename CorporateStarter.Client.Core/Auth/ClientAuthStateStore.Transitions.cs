using CorporateStarter.Client.Abstractions.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
    public sealed partial class ClientAuthStateStore
    {
        private static void ValidateTransition(
            ClientAuthSnapshot current,
            ClientAuthStatus next,
            Guid? userId,
            ClientSessionInvalidationReason? reason)
        {
            var allowed = current.Status switch
            {
                ClientAuthStatus.Initializing => next is
                    ClientAuthStatus.Authenticated or ClientAuthStatus.Anonymous or
                    ClientAuthStatus.Unavailable or ClientAuthStatus.UnsupportedEnvironment,
                ClientAuthStatus.Anonymous => next is
                    ClientAuthStatus.Revalidating or ClientAuthStatus.Anonymous or
                    ClientAuthStatus.UnsupportedEnvironment,
                ClientAuthStatus.Authenticated => next is
                    ClientAuthStatus.Revalidating or ClientAuthStatus.Anonymous,
                ClientAuthStatus.Revalidating => next is
                    ClientAuthStatus.Authenticated or ClientAuthStatus.Anonymous or
                    ClientAuthStatus.Unavailable or ClientAuthStatus.UnsupportedEnvironment,
                ClientAuthStatus.Unavailable => next is
                    ClientAuthStatus.Revalidating or ClientAuthStatus.Anonymous or
                    ClientAuthStatus.UnsupportedEnvironment,
                ClientAuthStatus.UnsupportedEnvironment => next is
                    ClientAuthStatus.Initializing or ClientAuthStatus.Anonymous,
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException(
                    $"Auth transition {current.Status} -> {next} is not allowed.");

            if (next is ClientAuthStatus.Revalidating or ClientAuthStatus.Unavailable
                && userId != current.UserId)
                throw new InvalidOperationException(
                    "Verification and temporary failure must preserve the previous user ID.");

            var identityChanged = current.UserId is not null
                && userId is not null && current.UserId != userId;

            if (identityChanged != (reason == ClientSessionInvalidationReason.IdentityChanged))
                throw new InvalidOperationException(
                    "A different user requires the IdentityChanged reason.");

            if (reason == ClientSessionInvalidationReason.IdentityChanged
                && next != ClientAuthStatus.Authenticated)
                throw new InvalidOperationException(
                    "IdentityChanged requires an authenticated destination.");

            if (reason is ClientSessionInvalidationReason.SignedOut
                    or ClientSessionInvalidationReason.SessionRejected
                && next != ClientAuthStatus.Anonymous)
                throw new InvalidOperationException(
                    "Session termination requires an anonymous destination.");

            if (current.UserId is not null && userId is null && reason is null)
                throw new InvalidOperationException(
                    "Removing the previous user requires an invalidation reason.");
        }
    }
}
