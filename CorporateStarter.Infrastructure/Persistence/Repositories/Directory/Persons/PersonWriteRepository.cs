using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Directory.Persons.Models;
using CorporateStarter.Core.Entities.Directory;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons
{
    public sealed class PersonWriteRepository : IPersonWriteRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IEntityAccess _access;

        public PersonWriteRepository(AppDbContext dbContext, IEntityAccess access)
        {
            _dbContext = dbContext;
            _access = access;
        }

        public async Task<Person> AddAsync(
            PersonWriteValues values,
            CancellationToken cancellationToken)
        {
            _access.For("persons").RequireStatusChange(true, values.IsActive);
            var person = values.Adapt<Person>();
            person.Id = Guid.NewGuid();
            person.CreatedAtUtc = DateTime.UtcNow;
            person.UpdatedAtUtc = null;

            await _dbContext.People.AddAsync(person, cancellationToken);

            return person;
        }

        public async Task<bool> UpdateAsync(
            PersonWriteValues values,
            CancellationToken cancellationToken)
        {
            if (values.Id is null)
                return false;

            var person = await _dbContext.People.VisibleTo(_access, "persons")
                .FirstOrDefaultAsync(x => x.Id == values.Id.Value, cancellationToken);

            if (person is null)
                return false;

            _access.For("persons").RequireStatusChange(person.IsActive, values.IsActive);
            values.Adapt(person);
            person.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var person = await _dbContext.People.VisibleTo(_access, "persons")
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (person is null)
                return false;

            person.IsActive = false;
            person.UpdatedAtUtc = DateTime.UtcNow;

            return true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
