using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Application.Directory.Companies.Models;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;


namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Companies
{
    public sealed class CompanyWriteRepository : ICompanyWriteRepository
    {
        private readonly AppDbContext _dbContext;

        public CompanyWriteRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Company> AddAsync(
            CompanyWriteValues values,
            CancellationToken cancellationToken)
        {
            var company = values.Adapt<Company>();
            company.Id = Guid.NewGuid();
            company.CreatedAtUtc = DateTime.UtcNow;
            company.UpdatedAtUtc = null;

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            return company;
        }

        public async Task<bool> UpdateAsync(
            CompanyWriteValues values,
            CancellationToken cancellationToken)
        {
            if (values.Id is null)
                return false;

            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (company is null)
                return false;

            values.Adapt(company);
            company.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (company is null)
                return false;

            company.IsActive = false;
            company.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
