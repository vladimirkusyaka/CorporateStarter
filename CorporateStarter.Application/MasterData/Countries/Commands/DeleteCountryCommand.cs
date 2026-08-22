using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.MasterData.Countries.Commands
{
    public sealed record DeleteCountryCommand(Guid Id)
        : IRequest<CommandResult<Unit>>;
}
