using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public enum MfaProviderType
    {
        Totp = 1,
        WebAuthn = 2,
        EmailCode = 3
    }
}
