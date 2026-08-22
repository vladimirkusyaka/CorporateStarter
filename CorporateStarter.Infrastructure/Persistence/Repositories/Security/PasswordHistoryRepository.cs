using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Core.Entities.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class PasswordHistoryRepository : IPasswordHistoryRepository
    {
        private readonly AppDbContext _dbContext;

        public PasswordHistoryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(
            Guid userId,
            int count,
            CancellationToken cancellationToken)
        {
            if (count <= 0)
            {
                return [];
            }

            return await _dbContext.PasswordHistories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(count)
                .Select(x => x.PasswordHash)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            PasswordHistory passwordHistory,
            CancellationToken cancellationToken)
        {
            await _dbContext.PasswordHistories.AddAsync(passwordHistory, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
