using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Application.MasterData.Cities.Queries
{
    public sealed record GetCityByIdQuery(Guid Id)
        : IRequest<CityDetailsDto?>;
}
