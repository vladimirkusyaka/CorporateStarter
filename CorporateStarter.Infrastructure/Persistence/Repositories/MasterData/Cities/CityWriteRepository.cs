using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
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
        private readonly IEntityAccess _access;

        public CityWriteRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<City> AddAsync(
            CityWriteValues values,
            CancellationToken cancellationToken)
        {
            _access.For("cities").RequireStatusChange(true, values.IsActive);
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

            var city = await _dbContext.Cities.VisibleTo(_access, "cities")
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (city is null)
                return false;

            _access.For("cities").RequireStatusChange(city.IsActive, values.IsActive);
            values.Adapt(city);
            city.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var city = await _dbContext.Cities.VisibleTo(_access, "cities")
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
