using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Shared.Dtos.Directory.Persons;


namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons
{
    public interface IPersonReadRepository
    {
        Task<IReadOnlyList<PersonListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<PersonDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
    }
}
