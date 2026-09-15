using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Browser.auth
{
    public sealed record BrowserCapabilities(
    bool IsSecureContext,
    bool HasWebLocks)
    {
        public bool IsSupported => IsSecureContext && HasWebLocks;
    }
}
