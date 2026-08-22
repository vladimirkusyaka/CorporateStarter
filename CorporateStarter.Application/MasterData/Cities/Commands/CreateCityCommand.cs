using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Application.MasterData.Cities.Commands
{
    public sealed record CreateCityCommand(CreateCityRequest Request)
        : IRequest<CommandResult<CityDetailsDto>>;
}
