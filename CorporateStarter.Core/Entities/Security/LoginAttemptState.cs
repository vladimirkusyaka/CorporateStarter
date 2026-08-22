using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Core.Entities.Security
{
    public sealed class LoginAttemptState
    {
        public Guid Id { get; set; }

        public string LoginIdentifierHash { get; set; } = string.Empty;

        public Guid? UserId { get; set; }

        public int FailedAttemptCount { get; set; }

        public DateTime FirstFailedAtUtc { get; set; }

        public DateTime LastFailedAtUtc { get; set; }

        public DateTime? LockedUntilUtc { get; set; }

        public string? LastIpAddress { get; set; }

        public string? LastUserAgent { get; set; }
    }
}
