using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries
{
    public interface ICountryReadRepository
    {
        Task<IReadOnlyList<CountryListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<CountryDetailsDto?> GetByIdAsync(
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
