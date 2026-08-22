using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Shared.Dtos.Permissions;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class RoleReadRepository(AppDbContext dbContext) : IRoleReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<RoleListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Roles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RoleListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsSystemRole = x.IsSystemRole,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<RoleDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Roles
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new RoleDetailsDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsSystemRole = x.IsSystemRole,
                    IsActive = x.IsActive,
                    Permissions = x.RolePermissions
                        .Where(rp => rp.Permission.IsActive)
                        .OrderBy(rp => rp.Permission.Group)
                        .ThenBy(rp => rp.Permission.Name)
                        .Select(rp => new PermissionListItemDto
                        {
                            Id = rp.Permission.Id,
                            Code = rp.Permission.Code,
                            Name = rp.Permission.Name,
                            Group = rp.Permission.Group,
                            Description = rp.Permission.Description,
                            IsActive = rp.Permission.IsActive
                        })
                        .ToArray()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return _dbContext.Roles
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalized = name.Trim();

            return _dbContext.Roles
                .AsNoTracking()
                .AnyAsync(
                    x => x.Name == normalized &&
                         (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        public async Task<bool> AllPermissionsExistAsync(
            IReadOnlyList<Guid> permissionIds,
            CancellationToken cancellationToken)
        {
            var ids = permissionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
                return true;

            var existingCount = await _dbContext.Permissions
                .AsNoTracking()
                .CountAsync(
                    x => x.IsActive && ids.Contains(x.Id),
                    cancellationToken);

            return existingCount == ids.Length;
        }

        public Task<bool> IsAdministratorRoleAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return _dbContext.Roles
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == id &&
                         x.Name == "Administrator",
                    cancellationToken);
        }
    }
}