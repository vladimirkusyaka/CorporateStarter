using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.MasterData.Cities.Models;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Cities
{
    public sealed class CityWriteRepository : ICityWriteRepository
    {
        private readonly AppDbContext _dbContext;

        public CityWriteRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<City> AddAsync(
            CityWriteValues values,
            CancellationToken cancellationToken)
        {
            var city = values.Adapt<City>();
            city.Id = Guid.NewGuid();
            city.CreatedAtUtc = DateTime.UtcNow;
            city.UpdatedAtUtc = null;

            await _dbContext.Cities.AddAsync(city, cancellationToken);

            return city;
        }

        public async Task<bool> UpdateAsync(
            CityWriteValues values,
            CancellationToken cancellationToken)
        {
            if (values.Id is null)
                return false;

            var city = await _dbContext.Cities
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (city is null)
                return false;

            values.Adapt(city);
            city.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var city = await _dbContext.Cities
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (city is null)
                return false;

            city.IsActive = false;
            city.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
