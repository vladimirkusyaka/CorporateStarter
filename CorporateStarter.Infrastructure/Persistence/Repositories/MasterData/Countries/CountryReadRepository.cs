using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries
{
    public sealed class CountryReadRepository : ICountryReadRepository
    {
        private readonly AppDbContext _dbContext;

        public CountryReadRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<CountryListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries
                .AsNoTracking()
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
                .Where(x => x.Id == id)
                .ProjectToType<CountryDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
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
