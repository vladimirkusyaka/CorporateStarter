using CorporateStarter.Core.Entities.Audit;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Audit
{
    public interface ISecurityAuditRepository
    {
        Task AddAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken);
    }
}
