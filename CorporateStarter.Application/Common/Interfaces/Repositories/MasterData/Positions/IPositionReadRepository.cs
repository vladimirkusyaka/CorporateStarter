using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;

public interface IPositionReadRepository
{
    Task<IReadOnlyList<PositionListItemDto>> GetListAsync(
        CancellationToken cancellationToken);

    Task<PositionDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken);
}
