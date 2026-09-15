using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public enum ClientAuthStatus
    {
        Initializing = 0,
        Anonymous = 1,
        Authenticated = 2,
        Revalidating = 3,
        Unavailable = 4,
        UnsupportedEnvironment = 5
    }
}
