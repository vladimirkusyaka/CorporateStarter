using CorporateStarter.Core.Entities.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface ILoginAttemptRepository
    {
        Task<LoginAttemptState?> GetByLoginIdentifierHashAsync(
            string loginIdentifierHash,
            CancellationToken cancellationToken);

        Task AddAsync(
            LoginAttemptState loginAttemptState,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
