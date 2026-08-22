using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface IRefreshTokenService
    {
        string GenerateToken();

        string HashToken(string token);
    }
}
