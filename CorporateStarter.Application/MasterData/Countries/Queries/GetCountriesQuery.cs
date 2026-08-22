using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Application.MasterData.Countries.Queries
{
    public sealed record GetCountriesQuery
    : IRequest<IReadOnlyList<CountryListItemDto>>;
}
