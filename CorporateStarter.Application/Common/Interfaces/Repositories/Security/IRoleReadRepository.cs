using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IRoleReadRepository
    {
        Task<IReadOnlyList<RoleListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<RoleDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludeId,
            CancellationToken cancellationToken);

        Task<bool> AllPermissionsExistAsync(
            IReadOnlyList<Guid> permissionIds,
            CancellationToken cancellationToken);

        Task<bool> IsAdministratorRoleAsync(
            Guid id,
            CancellationToken cancellationToken);
    }
}
