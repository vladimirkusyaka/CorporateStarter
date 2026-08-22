using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class LoginAttemptOptions
    {
        public const string SectionName = "LoginAttempts";

        public int WindowMinutes { get; set; } = 15;

        public int MaxFailedAttempts { get; set; } = 5;

        public int LockoutMinutes { get; set; } = 15;

        public int ProgressiveDelayBaseMilliseconds { get; set; } = 250;

        public int MaxProgressiveDelayMilliseconds { get; set; } = 3000;
    }
}
