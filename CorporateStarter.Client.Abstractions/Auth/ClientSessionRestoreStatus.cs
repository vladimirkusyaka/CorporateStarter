using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public enum ClientSessionRestoreStatus
    {
        Authenticated = 1,
        Anonymous = 2,
        Unavailable = 3,
        UnsupportedEnvironment = 4
    }
}
