using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class AuthSessionDto
    {
        public Guid Id { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime LastSeenAtUtc { get; set; }

        public string? CreatedByIp { get; set; }

        public string? UserAgent { get; set; }

        public string? DeviceName { get; set; }

        public bool IsCurrent { get; set; }
    }
}
