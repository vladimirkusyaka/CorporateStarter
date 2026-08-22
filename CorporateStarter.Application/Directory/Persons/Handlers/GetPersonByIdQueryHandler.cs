using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Directory.Persons.Queries;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Handlers
{
    public sealed class GetPersonByIdQueryHandler
        : IRequestHandler<GetPersonByIdQuery, PersonDetailsDto?>
    {
        private readonly IPersonReadRepository _repository;

        public GetPersonByIdQueryHandler(IPersonReadRepository repository)
        {
            _repository = repository;
        }

        public Task<PersonDetailsDto?> Handle(
            GetPersonByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
