using Mapster;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Shared.Dtos.Audit;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Audit
{
    public class AuditReadRepository(AppDbContext dbContext) : IAuditReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<AuditLogListItemDto>> GetListAsync(
                    CancellationToken cancellationToken)
        {
            return await _dbContext.AuditLogs
                .AsNoTracking()
                .OrderBy(x => x.EntityName)
                .ThenBy(x => x.Action)
                .ThenBy(x => x.CreatedAtUtc)
                .ProjectToType<AuditLogListItemDto>()
                .ToListAsync(cancellationToken);
        }
    }
}