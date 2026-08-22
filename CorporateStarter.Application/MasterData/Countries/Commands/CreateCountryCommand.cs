using System;
using System.Text;
using System.Collections.Generic;
using MediatR;using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.MasterData.Countries;


namespace CorporateStarter.Application.MasterData.Countries.Commands
{
    public sealed record CreateCountryCommand(CreateCountryRequest Request)
        : IRequest<CommandResult<CountryDetailsDto>>;
}
