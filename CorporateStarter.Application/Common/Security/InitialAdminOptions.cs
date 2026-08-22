using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class InitialAdminOptions
    {
        public const string SectionName = "InitialAdmin";

        public string Login { get; set; } = "admin";

        public string Email { get; set; } = "admin@corporatestarter.local";

        public string DisplayName { get; set; } = "Administrator";

        public string Password { get; set; } = string.Empty;
    }
}
