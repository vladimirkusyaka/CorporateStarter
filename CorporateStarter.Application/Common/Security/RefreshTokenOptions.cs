using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class RefreshTokenOptions
    {
        public const string SectionName = "RefreshTokens";

        public string CookieName { get; set; } = "__Host-corporate_starter_refresh";

        public int LifetimeDays { get; set; } = 14;

        public bool SecureCookie { get; set; } = true;

        public string SameSite { get; set; } = "Strict";
    }
}
