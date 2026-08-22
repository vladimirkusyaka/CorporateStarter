using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Application.Directory.Companies.Models;
using CorporateStarter.Core.Entities.Directory;


namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies
{
    public interface ICompanyWriteRepository
    {
        Task<Company> AddAsync(
            CompanyWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> UpdateAsync(
            CompanyWriteValues values,
            CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
