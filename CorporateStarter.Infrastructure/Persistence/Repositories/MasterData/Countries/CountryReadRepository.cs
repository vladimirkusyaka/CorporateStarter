using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Security;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries
{
    public sealed class CountryReadRepository : ICountryReadRepository
    {
        private readonly AppDbContext _dbContext;

        private readonly ICountryAccess _access;
        public CountryReadRepository(AppDbContext dbContext, ICountryAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<IReadOnlyList<CountryListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries
                .AsNoTracking()
                .Where(x => x.IsActive || _access.Capabilities.ViewInactive)
                .OrderBy(x => x.Name)
                .ProjectToType<CountryListItemDto>()
                .ToListAsync(cancellationToken);
        }

        public async Task<CountryDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries
                .AsNoTracking()
                .Where(x => x.Id == id && (x.IsActive || _access.Capabilities.ViewInactive))
                .ProjectToType<CountryDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries
                .AsNoTracking()
                .AnyAsync(x => x.Id == id && (x.IsActive || _access.Capabilities.ViewInactive), cancellationToken);
        }

        public async Task<bool> ExistsByCodeAsync(
            string code,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalizedCode = code.Trim().ToUpper();

            return await _dbContext.Countries
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Code.ToUpper() == normalizedCode &&
                    (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalizedName = name.Trim().ToLower();

            return await _dbContext.Countries
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Name.ToLower() == normalizedName &&
                    (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }
    }
}
