using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Shared.Dtos.Permissions;


namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class PermissionReadRepository(AppDbContext dbContext) : IPermissionReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<PermissionListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Code)
                .ProjectToType<PermissionListItemDto>()
                .ToListAsync(cancellationToken);
        }
    }
}
