using Mapster;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class UserReadRepository(AppDbContext dbContext) : IUserReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<UserListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Email)
                .Select(x => new UserListItemDto
                {
                    Id = x.Id,
                    Login = x.Login,
                    Email = x.Email,
                    DisplayName = x.DisplayName,
                    IsActive = x.IsActive,
                    PersonId = x.PersonId,
                    PersonName = x.Person == null
                        ? null
                        : x.Person.FirstName + " " + x.Person.LastName,
                    Roles = x.UserRoles
                        .Where(ur => ur.Role.IsActive)
                        .OrderBy(ur => ur.Role.Name)
                        .Select(ur => new UserRoleDto
                        {
                            RoleId = ur.Role.Id,
                            Name = ur.Role.Name
                        })
                        .ToArray()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<UserDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new UserDetailsDto
                {
                    Id = x.Id,
                    Login = x.Login,
                    Email = x.Email,
                    DisplayName = x.DisplayName,
                    IsActive = x.IsActive,
                    PersonId = x.PersonId,
                    PersonName = x.Person == null
                        ? null
                        : x.Person.FirstName + " " + x.Person.LastName,
                    Roles = x.UserRoles
                        .Where(ur => ur.Role.IsActive)
                        .OrderBy(ur => ur.Role.Name)
                        .Select(ur => new UserRoleDto
                        {
                            RoleId = ur.Role.Id,
                            Name = ur.Role.Name
                        })
                        .ToArray()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByLoginAsync(
            string login,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalized = login.Trim();

            return _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Login == normalized &&
                         (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLower();

            return _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Email.ToLower() == normalized &&
                         (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        public async Task<bool> AllRolesExistAsync(
            IReadOnlyList<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            var ids = roleIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
                return true;

            var existingCount = await _dbContext.Roles
                .AsNoTracking()
                .CountAsync(x => x.IsActive && ids.Contains(x.Id), cancellationToken);

            return existingCount == ids.Length;
        }

        public Task<bool> HasOtherActiveAdministratorAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id != userId &&
                         x.IsActive &&
                         x.UserRoles.Any(ur =>
                             ur.Role.IsActive &&
                             ur.Role.Name == "Administrator"),
                    cancellationToken);
        }

        public Task<bool> IsActiveAdministratorAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == userId &&
                         x.IsActive &&
                         x.UserRoles.Any(ur =>
                             ur.Role.IsActive &&
                             ur.Role.Name == "Administrator"),
                    cancellationToken);
        }
    }
}
