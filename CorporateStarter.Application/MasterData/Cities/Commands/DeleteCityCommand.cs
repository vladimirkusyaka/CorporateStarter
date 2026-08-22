using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.MasterData.Cities.Commands
{
    public sealed record DeleteCityCommand(Guid Id)
        : IRequest<CommandResult<Unit>>;
}
