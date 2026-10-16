using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons
{
    public class PersonReadRepository(AppDbContext dbContext, IEntityAccess access) : IPersonReadRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IEntityAccess _access = access;

        public async Task<IReadOnlyList<PersonListItemDto>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.People.VisibleTo(_access, "persons")
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
            return await _dbContext.People.VisibleTo(_access, "persons")
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
            return await _dbContext.People.VisibleTo(_access, "persons")
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
        }
    }
}
