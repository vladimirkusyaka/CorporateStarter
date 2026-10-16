using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
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
        private readonly IEntityAccess _access;

        public CompanyWriteRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<Company> AddAsync(
            CompanyWriteValues values,
            CancellationToken cancellationToken)
        {
            _access.For("companies").RequireStatusChange(true, values.IsActive);
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

            var company = await _dbContext.Companies.VisibleTo(_access, "companies")
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (company is null)
                return false;

            _access.For("companies").RequireStatusChange(company.IsActive, values.IsActive);
            values.Adapt(company);
            company.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var company = await _dbContext.Companies.VisibleTo(_access, "companies")
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
