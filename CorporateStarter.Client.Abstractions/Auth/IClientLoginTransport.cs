using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Abstractions.Auth
{
    public interface IClientLoginTransport
    {
        Task<ClientLoginResult> LoginAsync(
            string login,
            string password);
    }
}
