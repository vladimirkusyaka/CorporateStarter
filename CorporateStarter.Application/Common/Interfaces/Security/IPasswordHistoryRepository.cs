using CorporateStarter.Core.Entities.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface IPasswordHistoryRepository
    {
        Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(
            Guid userId,
            int count,
            CancellationToken cancellationToken);

        Task AddAsync(
            PasswordHistory passwordHistory,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
