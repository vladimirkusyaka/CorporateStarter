using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Core.Entities.Security
{
    public sealed class PasswordHistory
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
