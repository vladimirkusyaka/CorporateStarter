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

        private readonly IEntityAccess _access;
        public CountryReadRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<IReadOnlyList<CountryListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries.VisibleTo(_access, "countries")
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ProjectToType<CountryListItemDto>()
                .ToListAsync(cancellationToken);
        }

        public async Task<CountryDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries.VisibleTo(_access, "countries")
                .AsNoTracking()
                .Where(x => x.Id == id)
                .ProjectToType<CountryDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Countries.VisibleTo(_access, "countries")
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
