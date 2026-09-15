using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public sealed record ClientLoginResult
    {
        public ClientLoginStatus Status { get; }
        public Guid? UserId { get; }

        public ClientLoginResult(
            ClientLoginStatus status,
            Guid? userId = null)
        {
            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));

            if (userId == Guid.Empty ||
                (status == ClientLoginStatus.Authenticated) !=
                (userId is not null))
            {
                throw new ArgumentException(
                    "User ID must be present only for authenticated results.",
                    nameof(userId));
            }

            Status = status;
            UserId = userId;
        }
    }
}
