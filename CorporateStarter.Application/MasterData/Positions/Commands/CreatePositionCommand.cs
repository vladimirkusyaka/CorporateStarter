using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Application.MasterData.Positions.Commands
{
    public sealed record CreatePositionCommand(CreatePositionRequest Request)
        : IRequest<CommandResult<PositionDetailsDto>>;
}
