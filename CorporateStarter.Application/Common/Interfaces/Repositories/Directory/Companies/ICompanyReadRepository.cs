using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies
{
    public interface ICompanyReadRepository
    {
        Task<IReadOnlyList<CompanyListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<CompanyDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByCodeAsync(
            string code,
            Guid? excludeId,
            CancellationToken cancellationToken);

        Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludeId,
            CancellationToken cancellationToken);
    }
}
