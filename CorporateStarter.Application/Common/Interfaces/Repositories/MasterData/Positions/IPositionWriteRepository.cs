using CorporateStarter.Application.MasterData.Positions.Models;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;

public interface IPositionWriteRepository
{
    Task<Position> AddAsync(
        PositionWriteValues values,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        PositionWriteValues values,
        CancellationToken cancellationToken);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
