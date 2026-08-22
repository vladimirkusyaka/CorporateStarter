using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Core.Entities.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class LoginAttemptRepository : ILoginAttemptRepository
    {
        private readonly AppDbContext _dbContext;

        public LoginAttemptRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<LoginAttemptState?> GetByLoginIdentifierHashAsync(
            string loginIdentifierHash,
            CancellationToken cancellationToken)
        {
            return _dbContext.LoginAttemptStates
                .FirstOrDefaultAsync(x => x.LoginIdentifierHash == loginIdentifierHash, cancellationToken);
        }

        public async Task AddAsync(
            LoginAttemptState loginAttemptState,
            CancellationToken cancellationToken)
        {
            await _dbContext.LoginAttemptStates.AddAsync(loginAttemptState, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
