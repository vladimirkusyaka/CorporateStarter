using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public sealed class MfaEvaluationContext
    {
        public Guid UserId { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IReadOnlyList<string> Roles { get; set; } = [];

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}
