using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Audit
{
    public interface IAuditReadRepository
    {
        Task<IReadOnlyList<AuditLogListItemDto>> GetListAsync(
            CancellationToken cancellationToken);
    }
}
