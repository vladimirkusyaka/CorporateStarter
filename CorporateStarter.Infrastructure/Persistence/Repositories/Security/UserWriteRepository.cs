using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Users.Models;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class UserWriteRepository(AppDbContext dbContext) : IUserWriteRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Guid> AddAsync(
            UserWriteValues values,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = values.Login,
                Email = values.Email,
                DisplayName = values.DisplayName,
                PasswordHash = passwordHash,
                IsActive = values.IsActive,
                PersonId = values.PersonId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(user, cancellationToken);

            AddUserRoles(user.Id, values.RoleIds);

            return user.Id;
        }

        public async Task UpdateAsync(
            UserWriteValues values,
            CancellationToken cancellationToken)
        {
            if (!values.Id.HasValue)
                return;

            var user = await _dbContext.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (user is null)
                return;

            user.Login = values.Login;
            user.Email = values.Email;
            user.DisplayName = values.DisplayName;
            user.IsActive = values.IsActive;
            user.PersonId = values.PersonId;
            user.UpdatedAtUtc = DateTime.UtcNow;

            ReplaceUserRoles(user, values.RoleIds);
        }

        public async Task ChangePasswordAsync(
            Guid id,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (user is null)
                return;

            user.PasswordHash = passwordHash;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        public async Task DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (user is null)
                return;

            user.IsActive = false;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        private void AddUserRoles(
            Guid userId,
            IReadOnlyList<Guid> roleIds)
        {
            var userRoles = roleIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .Select(roleId => new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                })
                .ToArray();

            _dbContext.UserRoles.AddRange(userRoles);
        }

        private void ReplaceUserRoles(
            User user,
            IReadOnlyList<Guid> roleIds)
        {
            var newRoleIds = roleIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToHashSet();

            var currentRoleIds = user.UserRoles
                .Select(x => x.RoleId)
                .ToHashSet();

            var rolesToRemove = user.UserRoles
                .Where(x => !newRoleIds.Contains(x.RoleId))
                .ToArray();

            if (rolesToRemove.Length > 0)
                _dbContext.UserRoles.RemoveRange(rolesToRemove);

            var rolesToAdd = newRoleIds
                .Where(roleId => !currentRoleIds.Contains(roleId))
                .Select(roleId => new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                })
                .ToArray();

            if (rolesToAdd.Length > 0)
                _dbContext.UserRoles.AddRange(rolesToAdd);
        }
    }
}
