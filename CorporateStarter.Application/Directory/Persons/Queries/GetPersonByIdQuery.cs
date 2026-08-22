using MediatR;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Queries
{
    public sealed record GetPersonByIdQuery(Guid Id)
        : IRequest<PersonDetailsDto?>;
}
