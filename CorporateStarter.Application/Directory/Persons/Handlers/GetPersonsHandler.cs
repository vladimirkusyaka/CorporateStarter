using MediatR;
using CorporateStarter.Application.Directory.Persons.Queries;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Handlers
{
    public class GetPersonsHandler : IRequestHandler<GetPersonsQuery, IReadOnlyList<PersonListItemDto>>
    {
        private readonly IPersonReadRepository _repository;

        public GetPersonsHandler(IPersonReadRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<PersonListItemDto>> Handle(
            GetPersonsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetListAsync(cancellationToken);
        }
    }
}
