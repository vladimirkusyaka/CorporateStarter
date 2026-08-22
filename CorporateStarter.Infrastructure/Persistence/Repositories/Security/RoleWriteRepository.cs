using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Roles.Models;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class RoleWriteRepository(AppDbContext dbContext) : IRoleWriteRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Guid> AddAsync(
            RoleWriteValues values,
            CancellationToken cancellationToken)
        {
            var role = values.Adapt<Role>();

            role.Id = Guid.NewGuid();
            role.IsSystemRole = false;
            role.CreatedAtUtc = DateTime.UtcNow;

            await _dbContext.Roles.AddAsync(role, cancellationToken);

            AddRolePermissions(role.Id, values.PermissionIds);

            return role.Id;
        }

        public async Task UpdateAsync(
            RoleWriteValues values,
            CancellationToken cancellationToken)
        {
            if (!values.Id.HasValue)
                return;

            var role = await _dbContext.Roles
                .Include(x => x.RolePermissions)
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (role is null)
                return;

            role.Name = values.Name;
            role.Description = values.Description;
            role.IsActive = values.IsActive;
            role.UpdatedAtUtc = DateTime.UtcNow;

            ReplaceRolePermissions(role, values.PermissionIds);
        }

        public async Task DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (role is null)
                return;

            role.IsActive = false;
            role.UpdatedAtUtc = DateTime.UtcNow;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        private void AddRolePermissions(
            Guid roleId,
            IReadOnlyList<Guid> permissionIds)
        {
            var rolePermissions = permissionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .Select(permissionId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                })
                .ToArray();

            _dbContext.RolePermissions.AddRange(rolePermissions);
        }

        private void ReplaceRolePermissions(
            Role role,
            IReadOnlyList<Guid> permissionIds)
        {
            var newPermissionIds = permissionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToHashSet();

            var currentPermissionIds = role.RolePermissions
                .Select(x => x.PermissionId)
                .ToHashSet();

            var permissionsToRemove = role.RolePermissions
                .Where(x => !newPermissionIds.Contains(x.PermissionId))
                .ToArray();

            if (permissionsToRemove.Length > 0)
                _dbContext.RolePermissions.RemoveRange(permissionsToRemove);

            var permissionsToAdd = newPermissionIds
                .Where(permissionId => !currentPermissionIds.Contains(permissionId))
                .Select(permissionId => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                })
                .ToArray();

            if (permissionsToAdd.Length > 0)
                _dbContext.RolePermissions.AddRange(permissionsToAdd);
        }
    }
}
