using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities
{
    public interface ICityReadRepository
    {
        Task<IReadOnlyList<CityListItemDto>> GetListAsync(
            CancellationToken cancellationToken);

        Task<CityDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<bool> ExistsByNameAsync(
            Guid countryId,
            string name,
            string? region,
            Guid? excludeId,
            CancellationToken cancellationToken);
    }
}
