using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IUserReadRepository
    {
        Task<IReadOnlyList<UserListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<UserDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByLoginAsync(
            string login,
            Guid? excludeId,
            CancellationToken cancellationToken);

        Task<bool> ExistsByEmailAsync(
            string email,
            Guid? excludeId,
            CancellationToken cancellationToken);

        Task<bool> AllRolesExistAsync(
            IReadOnlyList<Guid> roleIds,
            CancellationToken cancellationToken);

        Task<bool> HasOtherActiveAdministratorAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> IsActiveAdministratorAsync(
            Guid userId,
            CancellationToken cancellationToken);
    }
}
