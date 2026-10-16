using CorporateStarter.Shared.Common;
using Mapster;
using CorporateStarter.Application.Common.Security;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.MasterData.Countries.Models;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries
{
    public sealed class CountryWriteRepository : ICountryWriteRepository
    {
        private readonly AppDbContext _dbContext;

        private readonly IEntityAccess _access;
        public CountryWriteRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<Country> AddAsync(
            CountryWriteValues values,
            CancellationToken cancellationToken)
        {
            if (!_access.For("countries").CanCreate) throw new UnauthorizedAccessException();
            var country = values.Adapt<Country>();
            country.IsActive = true;
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
            if (!_access.For("countries").CanUpdate) throw new UnauthorizedAccessException();
            if (values.Id is null)
                return false;

            var country = await _dbContext.Countries.VisibleTo(_access, "countries")
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (country is null)
                return false;

            if (values.IsActive is { } active && active != country.IsActive)
            {
                var caps = _access.For("countries");
                caps.RequireStatusChange(country.IsActive, active);
                country.IsActive = active;
            }
            country.Code = values.Code;
            country.Name = values.Name;
            country.NativeName = values.NativeName;
            country.PhoneCode = values.PhoneCode;
            values.IsActive = country.IsActive;
            country.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            if (!_access.For("countries").CanDelete) throw new UnauthorizedAccessException();
            var country = await _dbContext.Countries.VisibleTo(_access, "countries")
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
