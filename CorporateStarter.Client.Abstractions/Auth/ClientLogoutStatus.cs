using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public enum ClientLogoutStatus
    {
        SignedOut = 1,
        Unavailable,
        UnsupportedEnvironment,
        Busy
    }
}
