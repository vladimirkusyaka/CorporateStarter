using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons
{
    public class PersonReadRepository(AppDbContext dbContext) : IPersonReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<PersonListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.People
                .AsNoTracking()
                .Include(x => x.Company)
                .Include(x => x.Position)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.MiddleName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Code)
                .ProjectToType<PersonListItemDto>()
                .ToListAsync(cancellationToken);
        }

        public async Task<PersonDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.People
                .AsNoTracking()
                .Include(x => x.Company)
                .Include(x => x.Position)
                .Where(x => x.Id == id)
                .ProjectToType<PersonDetailsDto>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _dbContext.People
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }
    }
}
