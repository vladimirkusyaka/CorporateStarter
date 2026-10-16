using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.MasterData.Positions.Models;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Positions;

public sealed class PositionWriteRepository : IPositionWriteRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IEntityAccess _access;

    public PositionWriteRepository(AppDbContext dbContext, IEntityAccess access)
    { _dbContext = dbContext; _access = access; }

    public async Task<Position> AddAsync(
        PositionWriteValues values,
        CancellationToken cancellationToken)
    {
        _access.For("positions").RequireStatusChange(true, values.IsActive);
        var position = values.Adapt<Position>();
        position.Id = Guid.NewGuid();
        position.CreatedAtUtc = DateTime.UtcNow;
        position.UpdatedAtUtc = null;

        await _dbContext.Positions.AddAsync(position, cancellationToken);

        return position;
    }

    public async Task<bool> UpdateAsync(
        PositionWriteValues values,
        CancellationToken cancellationToken)
    {
        if (values.Id is null)
            return false;

        var position = await _dbContext.Positions.VisibleTo(_access, "positions")
            .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

        if (position is null)
            return false;

        _access.For("positions").RequireStatusChange(position.IsActive, values.IsActive);
        values.Adapt(position);
        position.UpdatedAtUtc = DateTime.UtcNow;

        return true;
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var position = await _dbContext.Positions.VisibleTo(_access, "positions")
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (position is null)
            return false;

        position.IsActive = false;
        position.UpdatedAtUtc = DateTime.UtcNow;

        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}