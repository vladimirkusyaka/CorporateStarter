using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Positions;

public sealed class PositionReadRepository : IPositionReadRepository
{
    private readonly AppDbContext _dbContext;

    public PositionReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PositionListItemDto>> GetListAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Positions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ProjectToType<PositionListItemDto>()
            .ToListAsync(cancellationToken);
    }

    public async Task<PositionDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Positions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .ProjectToType<PositionDetailsDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Positions
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLower();

        return await _dbContext.Positions
            .AsNoTracking()
            .AnyAsync(x =>
                x.Name.ToLower() == normalizedName &&
                (!excludeId.HasValue || x.Id != excludeId.Value),
                cancellationToken);
    }
}
