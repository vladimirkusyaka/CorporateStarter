using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Commands
{
    public sealed record CreatePersonCommand(CreatePersonRequest Request)
        : IRequest<CommandResult<PersonDetailsDto>>;
}
