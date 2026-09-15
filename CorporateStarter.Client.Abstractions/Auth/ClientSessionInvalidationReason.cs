using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public enum ClientSessionInvalidationReason
    {
        SignedOut = 1,
        SessionRejected = 2,
        IdentityChanged = 3
    }
}
