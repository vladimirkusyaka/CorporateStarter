using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public interface IClientAuthState
    {
        ClientAuthSnapshot Current { get; }

        event EventHandler<ClientAuthStateChangedEventArgs>? StateChanged;
    }
}
