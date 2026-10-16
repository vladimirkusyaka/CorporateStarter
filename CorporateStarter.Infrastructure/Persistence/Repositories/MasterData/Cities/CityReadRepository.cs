using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Cities
{
    public sealed class CityReadRepository : ICityReadRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IEntityAccess _access;

        public CityReadRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<IReadOnlyList<CityListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Cities.VisibleTo(_access, "cities")
                .AsNoTracking()
                .Include(x => x.Country)
                .OrderBy(x => x.Country.Name)
                .ThenBy(x => x.Name)
                .ProjectToType<CityListItemDto>()
                .ToListAsync(cancellationToken);
        }

        public async Task<CityDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Cities.VisibleTo(_access, "cities")
                .AsNoTracking()
                .Include(x => x.Country)
                .Where(x => x.Id == id)
                .ProjectToType<CityDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Cities.VisibleTo(_access, "cities")
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            Guid countryId,
            string name,
            string? region,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalizedName = name.Trim().ToLower();
            var normalizedRegion = NormalizeOptional(region)?.ToLower();

            return await _dbContext.Cities
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CountryId == countryId &&
                    x.Name.ToLower() == normalizedName &&
                    (x.Region == null ? normalizedRegion == null : x.Region.ToLower() == normalizedRegion) &&
                    (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
