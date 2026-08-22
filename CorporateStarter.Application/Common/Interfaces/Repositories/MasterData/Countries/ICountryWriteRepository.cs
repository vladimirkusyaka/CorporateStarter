using CorporateStarter.Application.MasterData.Countries.Models;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries
{
    public interface ICountryWriteRepository
    {
        Task<Country> AddAsync(
            CountryWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> UpdateAsync(
            CountryWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
