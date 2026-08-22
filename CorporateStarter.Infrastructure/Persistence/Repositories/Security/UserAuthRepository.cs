using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.Auth;


namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class UserAuthRepository : IUserAuthRepository
    {
        private readonly AppDbContext _dbContext;

        public UserAuthRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<UserAuthInfo?> GetByLoginOrEmailAsync(
            string loginOrEmail,
            CancellationToken cancellationToken)
        {
            var normalized = loginOrEmail.Trim();

            return GetUserAuthInfoQuery()
                .FirstOrDefaultAsync(
                    x => x.Login == normalized || x.Email == normalized,
                    cancellationToken);
        }

        public Task<UserAuthInfo?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return GetUserAuthInfoQuery()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);
        }

        private IQueryable<UserAuthInfo> GetUserAuthInfoQuery()
        {
            return _dbContext.Users
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new UserAuthInfo
                {
                    Id = x.Id,
                    Login = x.Login,
                    Email = x.Email,
                    DisplayName = x.DisplayName,
                    PasswordHash = x.PasswordHash,
                    Roles = x.UserRoles
                        .Where(ur => ur.Role.IsActive)
                        .OrderBy(ur => ur.Role.Name)
                        .Select(ur => new UserRoleProfileDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Permissions = ur.Role.RolePermissions
                                .Where(rp => rp.Permission.IsActive)
                                .OrderBy(rp => rp.Permission.Group)
                                .ThenBy(rp => rp.Permission.Name)
                                .Select(rp => new UserPermissionProfileDto
                                {
                                    Id = rp.Permission.Id,
                                    Code = rp.Permission.Code,
                                    Name = rp.Permission.Name,
                                    Group = rp.Permission.Group
                                })
                                .ToArray()
                        })
                        .ToArray()
                });
        }
    }
}
