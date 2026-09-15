using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public sealed record ClientAuthSnapshot
    {
        public static ClientAuthSnapshot Initial { get; } =
            new(ClientAuthStatus.Initializing, null, 0, 0);

        public ClientAuthStatus Status { get; }
        public Guid? UserId { get; }
        public long Revision { get; }
        public long SessionGeneration { get; }

        public ClientAuthSnapshot(
            ClientAuthStatus status,
            Guid? userId,
            long revision,
            long sessionGeneration)
        {
            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));

            ArgumentOutOfRangeException.ThrowIfNegative(revision);
            ArgumentOutOfRangeException.ThrowIfNegative(sessionGeneration);

            if (userId == Guid.Empty)
                throw new ArgumentException(
                    "User ID cannot be empty.", nameof(userId));

            if (status == ClientAuthStatus.Authenticated && userId is null)
                throw new ArgumentException(
                    "Authenticated state requires a user ID.", nameof(userId));

            if (userId is not null && status is (
                ClientAuthStatus.Initializing or
                ClientAuthStatus.Anonymous or
                ClientAuthStatus.UnsupportedEnvironment))
            {
                throw new ArgumentException(
                    "This state cannot contain a user ID.", nameof(userId));
            }

            Status = status;
            UserId = userId;
            Revision = revision;
            SessionGeneration = sessionGeneration;
        }
    }
}
