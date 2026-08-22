using CorporateStarter.Application.Security.Roles.Models;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IRoleWriteRepository
    {
        Task<Guid> AddAsync(
            RoleWriteValues values,
            CancellationToken cancellationToken);

        Task UpdateAsync(
            RoleWriteValues values,
            CancellationToken cancellationToken);

        Task DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
