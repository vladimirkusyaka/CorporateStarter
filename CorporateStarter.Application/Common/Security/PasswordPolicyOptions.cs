using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class PasswordPolicyOptions
    {
        public const string SectionName = "PasswordPolicy";

        public int MinimumLength { get; set; } = 12;

        public int MaximumLength { get; set; } = 128;

        public bool RequireUppercase { get; set; } = true;

        public bool RequireLowercase { get; set; } = true;

        public bool RequireDigit { get; set; } = true;

        public bool RequireNonAlphanumeric { get; set; } = true;

        public bool RejectLoginInPassword { get; set; } = true;

        public bool RejectEmailLocalPartInPassword { get; set; } = true;

        public bool RejectCommonPasswords { get; set; } = true;
    }
}
