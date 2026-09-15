using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public enum ClientLoginStatus
    {
        Authenticated = 1,
        Rejected,
        RateLimited,
        MfaRequired,
        Unavailable,
        UnsupportedEnvironment,
        InvalidInput,
        Busy
    }
}
