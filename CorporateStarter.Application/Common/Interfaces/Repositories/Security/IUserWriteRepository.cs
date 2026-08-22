using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Application.Security.Users.Models;


namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IUserWriteRepository
    {
        Task<Guid> AddAsync(
            UserWriteValues values,
            string passwordHash,
            CancellationToken cancellationToken);

        Task UpdateAsync(
            UserWriteValues values,
            CancellationToken cancellationToken);

        Task ChangePasswordAsync(
            Guid id,
            string passwordHash,
            CancellationToken cancellationToken);

        Task DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
