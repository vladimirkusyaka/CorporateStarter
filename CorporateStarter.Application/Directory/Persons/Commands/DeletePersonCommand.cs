using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.Directory.Persons.Commands
{
    public sealed record DeletePersonCommand(Guid Id)
        : IRequest<CommandResult<Unit>>;
}
