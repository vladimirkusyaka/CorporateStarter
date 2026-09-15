using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public interface IClientLogoutTransport
    {
        Task<ClientLogoutStatus> LogoutAsync();
    }
}
