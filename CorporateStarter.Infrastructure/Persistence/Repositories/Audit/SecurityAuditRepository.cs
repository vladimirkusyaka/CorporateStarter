using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Core.Entities.Audit;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Audit
{
    public sealed class SecurityAuditRepository : ISecurityAuditRepository
    {
        private readonly AppDbContext _dbContext;

        public SecurityAuditRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken)
        {
            await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        }
    }
}
