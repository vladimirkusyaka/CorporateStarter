using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.MasterData.Positions.Commands
{
    public sealed record DeletePositionCommand(Guid Id) : IRequest<CommandResult<Unit>>;
}
