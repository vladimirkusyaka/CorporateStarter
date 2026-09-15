using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public sealed record ClientSessionRestoreResult
    {
        public ClientSessionRestoreStatus Status { get; }
        public Guid? UserId { get; }

        private ClientSessionRestoreResult(
            ClientSessionRestoreStatus status,
            Guid? userId)
        {
            Status = status;
            UserId = userId;
        }

        public static ClientSessionRestoreResult Authenticated(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException(
                    "User ID cannot be empty.", nameof(userId));

            return new(ClientSessionRestoreStatus.Authenticated, userId);
        }

        public static ClientSessionRestoreResult Anonymous { get; } =
            new(ClientSessionRestoreStatus.Anonymous, null);

        public static ClientSessionRestoreResult Unavailable { get; } =
            new(ClientSessionRestoreStatus.Unavailable, null);

        public static ClientSessionRestoreResult UnsupportedEnvironment { get; } =
            new(ClientSessionRestoreStatus.UnsupportedEnvironment, null);
    }
}
