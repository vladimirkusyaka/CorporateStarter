using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Application.MasterData.Cities.Models;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities
{
    public interface ICityWriteRepository
    {
        Task<City> AddAsync(
            CityWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> UpdateAsync(
            CityWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
