using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        string? Login { get; }

        string? Email { get; }

        bool IsAuthenticated { get; }
    }
}
