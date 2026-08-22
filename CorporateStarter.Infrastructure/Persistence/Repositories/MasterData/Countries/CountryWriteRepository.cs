using Mapster;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.MasterData.Countries.Models;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries
{
    public sealed class CountryWriteRepository : ICountryWriteRepository
    {
        private readonly AppDbContext _dbContext;

        public CountryWriteRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Country> AddAsync(
            CountryWriteValues values,
            CancellationToken cancellationToken)
        {
            var country = values.Adapt<Country>();
            country.Id = Guid.NewGuid();
            country.CreatedAtUtc = DateTime.UtcNow;
            country.UpdatedAtUtc = null;

            await _dbContext.Countries.AddAsync(country, cancellationToken);

            return country;
        }

        public async Task<bool> UpdateAsync(
            CountryWriteValues values,
            CancellationToken cancellationToken)
        {
            if (values.Id is null)
                return false;

            var country = await _dbContext.Countries
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (country is null)
                return false;

            values.Adapt(country);
            country.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var country = await _dbContext.Countries
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (country is null)
                return false;

            country.IsActive = false;
            country.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
