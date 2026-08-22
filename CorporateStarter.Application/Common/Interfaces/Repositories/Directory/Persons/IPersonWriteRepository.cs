using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Application.Directory.Persons.Models;
using CorporateStarter.Core.Entities.Directory;


namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons
{
    public interface IPersonWriteRepository
    {
        Task<Person> AddAsync(
            PersonWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> UpdateAsync(
            PersonWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
