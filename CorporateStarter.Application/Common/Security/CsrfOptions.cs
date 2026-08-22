using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class CsrfOptions
    {
        public const string SectionName = "Csrf";

        public string CookieName { get; set; } = "__Host-corporate_starter_csrf";

        public string HeaderName { get; set; } = "X-CSRF-TOKEN";

        public bool SecureCookie { get; set; } = true;

        public string SameSite { get; set; } = "Strict";
    }
}
