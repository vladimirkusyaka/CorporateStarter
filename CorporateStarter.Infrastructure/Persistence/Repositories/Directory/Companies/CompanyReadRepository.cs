using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Companies
{
    public sealed class CompanyReadRepository : ICompanyReadRepository
    {
        private readonly AppDbContext _dbContext;

        public CompanyReadRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<CompanyListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.Companies
                .AsNoTracking()
                .Include(x => x.City)
                .ThenInclude(x => x!.Country)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Code)
                .ProjectToType<CompanyListItemDto>()
                .ToListAsync(cancellationToken);
        }

        public async Task<CompanyDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Companies
                .AsNoTracking()
                .Include(x => x.City)
                .ThenInclude(x => x!.Country)
                .Where(x => x.Id == id)
                .ProjectToType<CompanyDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Companies
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByCodeAsync(
            string code,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalizedCode = code.Trim().ToLower();

            return await _dbContext.Companies
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Code != null &&
                    x.Code.ToLower() == normalizedCode &&
                    (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var normalizedName = name.Trim().ToLower();

            return await _dbContext.Companies
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Name.ToLower() == normalizedName &&
                    (!excludeId.HasValue || x.Id != excludeId.Value),
                    cancellationToken);
        }
    }
}
